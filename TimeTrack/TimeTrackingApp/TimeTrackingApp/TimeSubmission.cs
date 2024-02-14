using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Xml.Linq;
using Page = PuppeteerSharp.Page;

namespace TimeTrackingApp
{
    public static class TimeSubmission
    {
        //private static DateTime date;
        private const string TEMPO_URL = "http://jira.edisoft.com:8080/secure/Tempo.jspa";
        private static string cachedUser = null;
        private static string cachedPass = null;
        private const string salt = "I'm not a security expert.";

        public static bool HasCachedCredentials()
        {
            return !string.IsNullOrEmpty(cachedUser);
        }

        public async static void StartSubmission(string username, string password, DateTime date, bool gui)
        {
            LogWindow logWindow = new LogWindow();

            logWindow.Show();
            logWindow.SetText(DateTime.Now + "\n"); // useful when comparing multiple log windows
            logWindow.SetText("Timesheet Log: " + date.ToString("dddd, dd MMMM yyyy") + "\n\n");

            // browser configs
            var launchOptions = new LaunchOptions
            {
                Headless = !gui, // = false for testing
                ExecutablePath = "C:/Program Files/Google/Chrome/Application/chrome.exe",
                DefaultViewport = null,
                Args = new[] { "--start-maximized" }
            };

            // open a new page in the browser
            var browser = await Puppeteer.LaunchAsync(launchOptions);
            var page = browser.PagesAsync().Result.First();

            logWindow.SetText("Browser Initialized!\n");

            // visit the tempo timesheet page
            try
            {
                logWindow.SetText("Loading tempo timesheet\n");
                logWindow.SetText(".....\n");
                await page.GoToAsync(TEMPO_URL);
            }
            catch (Exception ex)
            {
                await browser.CloseAsync();
                logWindow.SetText(ex.Message + "\n");
                MessageBox.Show("Please check your internet connection and try again!", "Connection Error");
                logWindow.Close();
                return;
            }
            
            await TempoLogin((Page)page, username, password, logWindow, date);
        }

        private static bool WaitForAnySelector(Page page, List<string> selectors, int timeLimit, bool visible = true)
        {
            var stopwatch = Stopwatch.StartNew();
            int pollPeriod = 200; // half second polling
            for (int i = 0; i < timeLimit / pollPeriod; i++)
            {
                page.WaitForTimeoutAsync(pollPeriod).Wait();
                foreach (var selector in selectors)
                {
                    try
                    {
                        page.WaitForSelectorAsync(selector, new WaitForSelectorOptions { Visible = visible, Timeout = 200 }).Wait();
                        // yeah this affects the total time but I found Timeout <100 caused bugs
                        return true;
                    }
                    catch { }
                }
                if (stopwatch.ElapsedMilliseconds > timeLimit)
                    return false;
            }
            return false;
        }

        private async static Task LoadTimesheetPopup(Page page, List<TimeEntry> timeEntries, LogWindow logWindow, DateTime date)
        {
            try
            {
                foreach (TimeEntry timeEntry in timeEntries)
                {
                    //// If the background grey screen is loaded, then wait N seconds until it is gone.
                    for (int i = 0; i < 20; i++)
                    {
                        try
                        {
                            await page.WaitForSelectorAsync(".sc-fONwsr.kbQTLr", new WaitForSelectorOptions { Timeout = 100 }); // grey background
                            await page.WaitForTimeoutAsync(500);
                            if (i == 0)
                            {
                                logWindow.SetText("\tWaiting for modal to close.");
                            }
                            else
                            {
                                logWindow.SetText("."); // cosmetic
                            }
                        }
                        catch (Exception ex)
                        {
                            if (i > 0)
                            {
                                logWindow.SetText("\n");
                            }
                            break;
                        }

                    }

                    logWindow.SetText("Entering time for " + timeEntry.Name + "\n");
                    await page.WaitForSelectorAsync("button[name=logWorkButton]");
                    await page.ClickAsync("button[name=logWorkButton]");

                    await page.WaitForSelectorAsync("input[id=issuePickerInput]", new WaitForSelectorOptions { Visible = true });
                    await page.ClickAsync("input[id=issuePickerInput]"); // click to focus
                    await page.TypeAsync("input[id=issuePickerInput]", timeEntry.Name);

                    logWindow.SetText("\tValidating Jira ID " + timeEntry.Name + "\n");

                    bool hasInvalidJira = WaitForAnySelector(page,
                        new List<string>
                        {
                            // .sc-hqyNC.cQYmRl appears multiple times: "Current Search" or "History Search"
                            // .sc-jbKcbu.fbJELm is the *sibling* that says 'No matching issues found'
                            "#issueDropdown > div > div:nth-child(2) > div.sc-jbKcbu.fbJELm", // "Current Search" -> 'No matching issues found'
                            ".sc-frDJqD.cXLmWf", // An issue with key 'MCS-13000' does not exist for field 'issue'.
                        }, 2000);

                    if (hasInvalidJira)
                    {
                        logWindow.SetText("\tSkipping " + timeEntry.Name + "\n");
                        await page.WaitForTimeoutAsync(100);
                        await page.ClickAsync(".sc-uJMKN.kvhaFl"); // Cancel button
                        await page.WaitForTimeoutAsync(100);
                        continue; // skip to next time entry
                    }

                    await page.WaitForTimeoutAsync(100);
                    await page.WaitForSelectorAsync("div.sc-kjoXOD.iGfzR"); // dropdown element
                    await page.ClickAsync("div.sc-kjoXOD.iGfzR");

                    // Another validation step to ensure the selected Jira entry is correct.
                    // E.g. "INT-1" must not match with INT-15
                    // E.g. "buying lunch" must not match with MCS-344, yes try it yourself to see
                    try
                    {
                        var element = await page.WaitForSelectorAsync($".sc-epnACN.gZrolZ"); // dropdown post-selection UI

                        var selectedJiraLabel = await page.EvaluateFunctionAsync<string>("e => e.textContent", element);

                        if (selectedJiraLabel != $"{timeEntry.Name?.ToUpper()}:")
                        {
                            throw new Exception();
                        }
                    }
                    catch
                    {
                        logWindow.SetText("\tSkipping " + timeEntry.Name + "\n");
                        await page.WaitForTimeoutAsync(100);
                        await page.ClickAsync(".sc-uJMKN.kvhaFl"); // Cancel button
                        await page.WaitForTimeoutAsync(100);
                        continue; // skip to next time entry
                    }

                    await page.WaitForTimeoutAsync(100);
                    await page.WaitForSelectorAsync("textarea[id=comment]");
                    await page.TypeAsync("textarea[id=comment]", timeEntry.Details.Replace("\r", ""));

                    await page.WaitForTimeoutAsync(100);
                    await page.WaitForSelectorAsync("input[id=started]");
                    await page.ClickAsync("input[id=started]"); // click to focus

                    // erase default value (current date) before replacing with correct date
                    await page.ClickAsync("#started");
                    await page.Keyboard.DownAsync("Control");
                    await page.Keyboard.PressAsync("a");
                    await page.Keyboard.UpAsync("Control");
                    await page.Keyboard.PressAsync("Backspace");


                    await page.TypeAsync("input[id=started]", date.ToString("dd MMM yyyy").Replace(" ", "/"));
                    await page.Keyboard.PressAsync("Enter");

                    await page.WaitForTimeoutAsync(100);
                    await page.WaitForSelectorAsync("input[id=timeSpentSeconds]");
                    await page.ClickAsync("input[id=timeSpentSeconds]"); // click to focus
                    await page.TypeAsync("input[id=timeSpentSeconds]", timeEntry.DurationHours.ToString());

                    if (timeEntry.Name.ToUpper().Substring(0, 3) != "INT")
                    {
                        await page.WaitForTimeoutAsync(100);
                        await page.WaitForSelectorAsync("input[id=timeSpentSeconds]");
                        await page.TypeAsync("input[id=billable]", "0h");
                    }
                    await page.ClickAsync("button[name=submitWorklogButton]");
                }

                // Only write the success message if all time entries were processed successfully.
                logWindow.SetText(".....\n");
                logWindow.SetText("SUCCESSFULLY FILLED :D, Visit Tempo to verify all the entries!\n");
                var response = MessageBox.Show("Your timesheet has been filled successfully!\nDo you want to keep the log window open?", "Timesheet Filled", MessageBoxButton.YesNo);
                if (response == MessageBoxResult.No)
                {
                    logWindow.Close();
                }
            }
            catch (Exception ex)
            {
                logWindow.SetText(ex.Message + "\n");
                MessageBox.Show("Alert: Review the log window details.", "Error occurred");
                return;
            }
        }

        private static string Encrypt(string text)
        {
            // https://stackoverflow.com/questions/9031537/really-simple-encryption-with-c-sharp-and-symmetricalgorithm
            // Good enough for now

            return Convert.ToBase64String(
                ProtectedData.Protect(
                    Encoding.Unicode.GetBytes(text),
                    Encoding.Unicode.GetBytes(salt),
                    DataProtectionScope.CurrentUser));
        }
        private static string Decrypt(string text)
        {
            return Encoding.Unicode.GetString(
                ProtectedData.Unprotect(
                    Convert.FromBase64String(text),
                    Encoding.Unicode.GetBytes(salt),
                    DataProtectionScope.CurrentUser));

        }

        private async static Task TempoLogin(Page page, string username, string password, LogWindow logWindow, DateTime date)
        {
            try
            {
                if (username == null && password == null)
                {
                    username = Decrypt(cachedUser);
                    password = Decrypt(cachedPass);

                    logWindow.SetText("Using cached credentials from last successful login.\n");
                }

                await page.WaitForSelectorAsync("input[name=os_username]");
                await page.WaitForSelectorAsync("input[name=os_password]");
                await page.WaitForSelectorAsync("input[name=login]");
                await page.TypeAsync("input[name=os_username]", username);
                await page.TypeAsync("input[name=os_password]", password);
                await page.ClickAsync("input[name=login]");

                // Detect either the timesheet screen or the invalid-login screen.
                int _loginWaitLimit = 10;
                for (int i = 0; i < 10; i++)
                {
                    try // look for timesheet page
                    {
                        await page.WaitForSelectorAsync("button[name=logWorkButton]", new WaitForSelectorOptions { Timeout = 1000 });
                        logWindow.SetText("Login successful.\n");

                        cachedUser = Encrypt(username);
                        cachedPass = Encrypt(password);

                        break;
                    }
                    catch { }

                    try // look for invalid-login screen
                    {
                        await page.WaitForSelectorAsync("#login-form > div.form-body > div.aui-message.aui-message-error > p", new WaitForSelectorOptions { Timeout = 1000 });
                        await page.CloseAsync();
                        logWindow.SetText("Invalid credentials :(\n");

                        username = null;
                        password = null;

                        MessageBox.Show("Please check your username and password and try again!", "Login Error");
                        //logWindow.Close();
                        return;
                    }
                    catch { }

                    if (i == _loginWaitLimit - 1)
                    {
                        logWindow.SetText("Timed out during login process.\n");
                        MessageBox.Show("Please try again later.", "Login Error");
                        //logWindow.Close();
                        return;
                    }
                }

                List<TimeEntry> timeEntries = SharedCommon.LoadTimeEntries(date);
                await LoadTimesheetPopup(page, timeEntries, logWindow, date);

                await page.CloseAsync();
                //logWindow.Close();
            }
            catch (Exception ex)
            {
                await page.CloseAsync();
                logWindow.SetText(ex.Message + "\n");
                MessageBox.Show("There was an error logging your timesheet!", "Timesheet Logging Error");
                //logWindow.Close();
            }
        }
    }
}
