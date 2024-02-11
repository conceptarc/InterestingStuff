using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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
        private static DateTime date;
        private const string TEMPO_URL = "http://jira.edisoft.com:8080/secure/Tempo.jspa";

        public async static void DoSubmission(string username, string password, DateTime currentViewingDate, bool gui)
        {
            LogWindow logWindow = new LogWindow();

            date = currentViewingDate;

            logWindow.Show();
            logWindow.SetText("Timesheet Log: " + date.ToString("dddd, dd MMMM yyyy") + "\n\n");

            // browser configs
            var launchOptions = new LaunchOptions
            {
                Headless = gui, // = false for testing
                ExecutablePath = "C:/Program Files/Google/Chrome/Application/chrome.exe"
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
            await TempoLogin((Page)page, username, password, logWindow);
            
        }

        private async static Task LoadTimesheetPopup(Page page, List<TimeEntry> timeEntries, LogWindow logWindow)
        {
            try
            {
                //// If the background grey screen is loaded, then wait N seconds until it is gone.
                for (int i = 0; i < 20; i++)
                {
                    try
                    {
                        await page.WaitForSelectorAsync(".sc-fONwsr.kbQTLr", new WaitForSelectorOptions { Timeout = 100 }); // grey background
                        await page.WaitForTimeoutAsync(1000);
                        if (i == 0)
                        {
                            logWindow.SetText("\tWaiting for modal to close.");
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

                foreach (TimeEntry timeEntry in timeEntries)
                {
                    logWindow.SetText("Entering time for " + timeEntry.Name + "\n");
                    await page.WaitForSelectorAsync("button[name=logWorkButton]");
                    await page.ClickAsync("button[name=logWorkButton]");
                    
                    await page.WaitForTimeoutAsync(100);
                    await page.WaitForSelectorAsync("input[id=issuePickerInput]");
                    await page.ClickAsync("input[id=issuePickerInput]"); // click to focus
                    await page.TypeAsync("input[id=issuePickerInput]", timeEntry.Name);

                    logWindow.SetText("\tValidating Jira ID " + timeEntry.Name + "\n");

                    try
                    {
                        // .sc-hqyNC.cQYmRl appears multiple times: "Current Search" or "History Search"
                        // .sc-jbKcbu.fbJELm is the *sibling* that says 'No matching issues found'
                        await page.WaitForSelectorAsync("#issueDropdown > div > div:nth-child(2) > div.sc-jbKcbu.fbJELm",
                            new WaitForSelectorOptions { Visible = true, Timeout = 2000 }); // No matching issues found

                        // "Current Search" -> 'No matching issues found'
                        logWindow.SetText("\tSkipping " + timeEntry.Name + "\n");
                        await page.ClickAsync(".sc-uJMKN.kvhaFl");
                        continue; // skip to next time entry
                    }
                    catch { }


                    await page.WaitForTimeoutAsync(100);
                    await page.WaitForSelectorAsync("div.sc-kjoXOD.iGfzR"); // dropdown element
                    await page.ClickAsync("div.sc-kjoXOD.iGfzR");

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

                    if (timeEntry.Name.Substring(0, 3) != "INT" && timeEntry.Name.Substring(0, 3) != "int")
                    {
                        await page.WaitForTimeoutAsync(100);
                        await page.WaitForSelectorAsync("input[id=timeSpentSeconds]");
                        await page.TypeAsync("input[id=billable]", "0h");
                    }
                    await page.WaitForTimeoutAsync(2000);
                    await page.ClickAsync("button[name=submitWorklogButton]");
                    await page.WaitForTimeoutAsync(2000);
                }
            }
            catch (Exception ex)
            {
                logWindow.SetText(ex.Message + "\n");
                return;
            }
        }

        private async static Task TempoLogin(Page page, string username, string password, LogWindow logWindow)
        {
            try
            {
                await page.WaitForSelectorAsync("input[name=os_username]");
                await page.WaitForSelectorAsync("input[name=os_password]");
                await page.WaitForSelectorAsync("input[name=login]");
                await page.TypeAsync("input[name=os_username]", username);
                await page.TypeAsync("input[name=os_password]", password);
                await page.ClickAsync("input[name=login]");

                // show error for incorrect login
                try
                {
                    await page.WaitForSelectorAsync("#login-form > div.form-body > div.aui-message.aui-message-error > p", new WaitForSelectorOptions { Timeout = 5000 });
                    await page.CloseAsync();
                    logWindow.SetText("Invalid credentials :(\n");
                    MessageBox.Show("Please check your username and password and try again!", "Login Error");
                    logWindow.Close();
                    return;
                }
                catch (Exception ex) { }

                List<TimeEntry> timeEntries = SharedCommon.LoadTimeEntries(date);
                await LoadTimesheetPopup(page, timeEntries, logWindow);

                await page.CloseAsync();
                logWindow.SetText(".....\n");
                logWindow.SetText("SUCCESSFULLY FILLED :D, Visit Tempo to verify all the entries!\n");
                MessageBox.Show("Your timesheet has been filled successfully!", "Timesheet Filled");
                logWindow.Close();
                return;
            }
            catch (Exception ex)
            {
                await page.CloseAsync();
                logWindow.SetText(ex.Message + "\n");
                MessageBox.Show("There was an error logging your timesheet!", "Timesheet Logging Error");
                logWindow.Close();
                return;
            }
        }
    }
}
