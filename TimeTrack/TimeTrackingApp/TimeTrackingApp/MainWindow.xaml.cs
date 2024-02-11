using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.Threading;
using System.Globalization;
using System.IO;
using Hardcodet.Wpf;
using Hardcodet.Wpf.TaskbarNotification;
using Newtonsoft.Json;

namespace TimeTrackingApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TaskbarIcon taskBarIcon;
        private ObservableCollection<TimeEntryView> viewCollection;
        public List<TimeEntry> CurrentTimeEntries { get; set; }
        private Dictionary<int, TimeEntry> entryByIds;
        private EntryDetails detailsView;
        private string dateString;

        private DateTime currentViewingDate;
        private bool pauseUI; // kind of used like a mutex

        public MainWindow()
        {
            InitializeComponent();

            currentViewingDate = DateTime.Now;

            InitTaskbar();

            InitDataGrid();

            detailsView = new EntryDetails();
            detailsView.Hide();

            // Init title bar with current date
            UpdateViewingDate();
        }

        // Set up the taskbar icon and its right-click menu option
        private void InitTaskbar()
        {
            ContextMenu rightClickMenu = new ContextMenu();
            MenuItem closeOption = new MenuItem { Header = "Close" };
            closeOption.Click += new RoutedEventHandler(
                (object sendr, RoutedEventArgs eventArgs) =>
                {
                    SaveImmediately();
                    Environment.Exit(0); // use this over Application.Current.Shutdown()
                });

            rightClickMenu.Items.Add(closeOption);

            taskBarIcon = new TaskbarIcon
            {
                Icon = SystemIcons.Information, // no custom icon for now
                ToolTipText = "Time Tracker App",
                Visibility = Visibility.Visible,
                ContextMenu = new ContextMenu()
            };
            taskBarIcon.ContextMenu = rightClickMenu;

            taskBarIcon.TrayMouseDoubleClick += new RoutedEventHandler(
                (object sendr, RoutedEventArgs eventArgs) =>
                {
                    this.Show();
                });

            taskBarIcon.TrayBalloonTipClicked += new RoutedEventHandler(
                (object sendr, RoutedEventArgs eventArgs) =>
                {
                    //this.Show(); // same as TrayMouseDoubleClick
                });
        }

        // Set up the data grid
        private void InitDataGrid()
        {
            TableGrid.IsReadOnly = true;
            TableGrid.SelectionMode = DataGridSelectionMode.Single;
            TableGrid.MinRowHeight = 50;

            viewCollection = new ObservableCollection<TimeEntryView>();
            CurrentTimeEntries = SharedCommon.LoadTimeEntries(currentViewingDate);
            entryByIds = new Dictionary<int, TimeEntry>();

            RefreshViewCollection();

            this.TableGrid.ItemsSource = viewCollection;

            Task.Factory.StartNew(() =>
            {
                RenderTableGrid();
            });

            // Delayed configuration of column width to fit the last column to the end of the screen.
            Task.Factory.StartNew(() =>
            {
                while (this.TableGrid.Columns.Count == 0)
                {
                    Thread.Sleep(100);
                }
                this.Dispatcher.Invoke(() =>
                {
                    // [3] is Details
                    this.TableGrid.Columns[3].Width = new DataGridLength(70, DataGridLengthUnitType.Star);

                    //https://stackoverflow.com/questions/35543044/wpf-datagrid-column-textwrapping-programmatically
                    var style = new Style(typeof(TextBlock));
                    style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
                    // style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));

                    (TableGrid.Columns[3] as DataGridTextColumn).ElementStyle = style;
                });
            });

            // Start task for auto-saving
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    Thread.Sleep(10000); // 10 sec
                    SaveImmediately();
                }
            });
        }

        private void RefreshViewCollection(bool openNewEntry = true)
        {
            // Merge / update differences between the currentTimeEntries and viewCollection

            // Use reverse for-int loop so we can modify the collection.
            for (int i = viewCollection.Count - 1; i >= 0; i--)
            {
                TimeEntryView view = viewCollection[i];
                int id = view.GetId();
                if (!entryByIds.ContainsKey(id))
                    continue;

                // check if we deleted a record from CurrentTimeEntries
                // viewCollection could be out of date here
                if (!CurrentTimeEntries.Contains(entryByIds[id]))
                {
                    viewCollection.RemoveAt(i);
                    continue;
                }

                // update row details every tick
                TimeEntryView newView = entryByIds[id].ToView(id);
                view.Active = newView.Active;
                view.Hours = newView.Hours;
                view.Name = newView.Name;
                view.Details = newView.Details;
            }

            // Insert new
            foreach (TimeEntry entry in CurrentTimeEntries)
            {
                if (entryByIds.ContainsValue(entry))
                    continue;

                int newId = entryByIds.Keys.Count + 1;
                entryByIds[newId] = entry;
                var newRow = entry.ToView(newId);
                viewCollection.Add(newRow);

                if (openNewEntry)
                {
                    // autoselect newest entry
                    TableGrid.SelectedItem = newRow;
                    TableGrid.ScrollIntoView(newRow);
                    DisplayDetails(true); // trigger Edit window
                }
            }

            //viewCollection.Clear();
            //foreach (TimeEntry entry in currentTimeEntries)
            //{
            //    viewCollection.Add(entry.ToView());
            //}

            TotalHours.Text = CurrentTimeEntries.Sum(m => m.DurationHours)
                .ToString("0.00") + " total hours";
        }

        private void RenderTableGrid()
        {
            while (true)
            {
                this.Dispatcher.Invoke(() =>
                {
                    if (!pauseUI)
                    {
                        RefreshViewCollection();
                    }
                    //TableGrid.Items.Refresh();
                    //CollectionViewSource.GetDefaultView(TableGrid.ItemsSource).Refresh();
                });
                Thread.Sleep(50);
            }
        }

        private void UpdateViewingDate()
        {
            pauseUI = true;

            // Init title bar with current date
            DateTime n = currentViewingDate;

            string GetDaySuffix(int day)
            {
                // https://stackoverflow.com/questions/2050805/getting-day-suffix-when-using-datetime-tostring
                switch (day)
                {
                    case 1:
                    case 21:
                    case 31:
                        return "st";
                    case 2:
                    case 22:
                        return "nd";
                    case 3:
                    case 23:
                        return "rd";
                    default:
                        return "th";
                }
            }
            CurrentDateTime.Text = $"{n.DayOfWeek.ToString()} {n.ToString("MMMM dd")}" +
                $"{GetDaySuffix(n.Day)} {n.ToString("yyyy")}";

            dateString = DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

            this.Dispatcher.Invoke(() =>
            {
                RefreshViewCollection(false);
                //TableGrid.Items.Refresh();
                //CollectionViewSource.GetDefaultView(TableGrid.ItemsSource).Refresh();
                pauseUI = false;
            });
        }

        private void SaveTimeEntries(List<TimeEntry> timeEntries)
        {
            if (!timeEntries.Any())
                return; // don't create a bunch of empty files (esp. when navigating dates)

            string fileName = SharedCommon.GetFileName(currentViewingDate);
            string jsonData = JsonConvert.SerializeObject(timeEntries);

            try
            {
                File.WriteAllText(fileName, jsonData); // save attempt conflicted
            }
            catch (Exception e)
            {
            } // just try again later
        }

        public void SaveImmediately()
        {
            // check if the day has rolled over
            string newDateString = DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            if (newDateString != dateString)
            {
                dateString = newDateString;
                // Bug fix: when leaving this program overnight, it will save the previous
                // day's data into the next day's data file. Solution: do not save the 
                // previous day's completed time entries.
                lock (CurrentTimeEntries)
                {
                    for (int i = CurrentTimeEntries.Count - 1; i >= 0; i--)
                    {
                        TimeEntry entry = CurrentTimeEntries[i];
                        if (!entry.IsActive)
                        {
                            CurrentTimeEntries.RemoveAt(i);
                        }
                    }
                }

                // Don't forget to update the display.
                currentViewingDate = DateTime.Now;
                UpdateViewingDate();
            }

            SaveTimeEntries(CurrentTimeEntries);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;

            this.Hide();

            taskBarIcon.ShowBalloonTip("Time Tracker App",
                "Minimized to task tray.", BalloonIcon.Info);
        }

        #region Create new time tracking entry

        private void NewEntry_Click(object sender, RoutedEventArgs e)
        {
            CreateNewEntry();
        }

        private void WindowScreen_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.N)
                CreateNewEntry();

            if (e.Key == Key.S) // start / stop
                ToggleEntryTimer();

            if (e.Key == Key.E) // edit
                DisplayDetails();
        }

        private void CreateNewEntry()
        {
            TimeEntry activeEntry = CurrentTimeEntries.FirstOrDefault(m => m.IsActive);
            if (activeEntry != null)
                activeEntry.StopTimer();

            TimeEntry newEntry = new TimeEntry
            {
                UniqueId = Guid.NewGuid(),
                Name = "(new) Created: " + DateTime.Now.ToString("h:mm:ss tt"),
                Details = ""
            };
            CurrentTimeEntries.Add(newEntry);
            newEntry.StartTimer();
            SaveImmediately();
        }

        #endregion

        #region Switch timer

        private void TableGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void TableGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //TimeEntryView selectedViewEntry = e.AddedItems[0] as TimeEntryView;
            //TimeEntry newEntry = entryByIds[selectedViewEntry.GetId()];

            //SwitchTimer(newEntry);
        }

        private void ToggleTimer_Click(object sender, RoutedEventArgs e)
        {
            ToggleEntryTimer();
        }

        private void ToggleEntryTimer()
        {
            if (TableGrid.SelectedItem == null)
                return;

            TimeEntryView selectedViewEntry = TableGrid.SelectedItem as TimeEntryView;
            TimeEntry selectedEntry = entryByIds[selectedViewEntry.GetId()];

            if (selectedEntry.IsActive)
                selectedEntry.StopTimer();
            else
                SwitchTimer(selectedEntry);

            SaveImmediately();
        }

        private void SwitchTimer(TimeEntry newEntry)
        {
            TimeEntry activeEntry = CurrentTimeEntries.FirstOrDefault(m => m.IsActive);
            if (activeEntry != null)
                activeEntry.StopTimer();

            newEntry.StartTimer();
        }

        #endregion

        #region Edit Entry / Text

        private void EditEntry_Click(object sender, RoutedEventArgs e)
        {
            DisplayDetails();
        }

        private void DisplayDetails(bool highlightName = false)
        {
            if (TableGrid.SelectedItem == null)
                return;

            if (!detailsView.CloseWithoutSave()) // close previous work
            {
                return;
            }

            detailsView.MainWindow = this;
            TimeEntryView selectedViewEntry = TableGrid.SelectedItem as TimeEntryView;
            TimeEntry selectedEntry = entryByIds[selectedViewEntry.GetId()];

            detailsView.Show();
            detailsView.Focus();
            detailsView.SelectedTimeEntry = selectedEntry;
            // set all the text values on the screen
            detailsView.NameField.Text = selectedEntry.Name;
            detailsView.DetailsField.Text = selectedEntry.Details;
            detailsView.TimeStatusText.Text = selectedEntry.IsActive ? "Yes" : "No";
            detailsView.CurrentTime.Text = selectedEntry.ToView(0).Hours;

            detailsView.PendingOffset = 0;

            if (highlightName)
            {
                detailsView.NameField.Focus();
                detailsView.NameField.SelectAll();
            }
        }

        #endregion

        private void PrevDay_Click(object sender, RoutedEventArgs e)
        {
            SaveImmediately();
            currentViewingDate = currentViewingDate.AddDays(-1);
            CurrentTimeEntries = SharedCommon.LoadTimeEntries(currentViewingDate);
            UpdateViewingDate();
        }

        private void NextDay_Click(object sender, RoutedEventArgs e)
        {
            SaveImmediately();
            currentViewingDate = currentViewingDate.AddDays(1);
            CurrentTimeEntries = SharedCommon.LoadTimeEntries(currentViewingDate);
            UpdateViewingDate();
        }

        private void ReturnToNow_Click(object sender, RoutedEventArgs e)
        {
            SaveImmediately();
            currentViewingDate = DateTime.Now;
            CurrentTimeEntries = SharedCommon.LoadTimeEntries(currentViewingDate);
            UpdateViewingDate();
        }

        private void SubmitToday_Click(object sender, RoutedEventArgs e)
        {
            // check if there are any entries to fill for today 
            var timeEntries = SharedCommon.LoadTimeEntries(currentViewingDate);
            if (timeEntries.Count > 0)
            {
                // check if there are any entries with active timer
                if (timeEntries.Any(m => m.IsActive))
                {
                    MessageBox.Show("Please stop the timer and try again!", "Timer active");
                    return;
                }
                    
            }
            else
            {
                MessageBox.Show("There are no entries to fill out for today!", "No entries found");
                return;
            }
            LoginWindow loginWindow = new LoginWindow(currentViewingDate, !(bool)GUISelect.IsChecked);
            loginWindow.Show();
        }
    }
}
