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

        public MainWindow()
        {
            InitializeComponent();

            InitTaskbar();

            InitDataGrid();

            detailsView = new EntryDetails();
            detailsView.Hide();

            // Init title bar with current date
            DateTime n = DateTime.Now;
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
        }

        // Set up the taskbar icon and its right-click menu option
        private void InitTaskbar()
        {
            ContextMenu rightClickMenu = new ContextMenu();
            MenuItem closeOption = new MenuItem { Header = "Close" };
            closeOption.Click += new RoutedEventHandler(
                (object sendr, RoutedEventArgs eventArgs) =>
                {
                    SaveTimeEntries(CurrentTimeEntries);
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
            CurrentTimeEntries = LoadTimeEntries();
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
                    Thread.Sleep(15000); // 15 sec
                    SaveTimeEntries(CurrentTimeEntries);
                }
            });
        }

        private void RefreshViewCollection()
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

                // autoselect newest entry
                TableGrid.SelectedItem = newRow;
                TableGrid.ScrollIntoView(newRow);
                DisplayDetails(true); // trigger Edit window
            }

            //viewCollection.Clear();
            //foreach (TimeEntry entry in currentTimeEntries)
            //{
            //    viewCollection.Add(entry.ToView());
            //}
        }

        private void RenderTableGrid()
        {
            while (true)
            {
                this.Dispatcher.Invoke(() =>
                {
                    RefreshViewCollection();
                    //TableGrid.Items.Refresh();
                    //CollectionViewSource.GetDefaultView(TableGrid.ItemsSource).Refresh();
                });
                Thread.Sleep(50);
            }
        }

        private string GetFileName()
        {
            // No database for now; we will just store data into a text file (1 file per day)
            string suffix = DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            string fileName = $"TimeData_{suffix}.dat";

            return fileName;
        }

        private List<TimeEntry> LoadTimeEntries()
        {
            // No database for now; we will just store data into a text file (1 file per day)
            string fileName = GetFileName();

            if (!File.Exists(fileName))
                return new List<TimeEntry>();

            try
            {
                string fileJson = File.ReadAllText(fileName);
                var entries = JsonConvert.DeserializeObject<List<TimeEntry>>(fileJson);

                return entries ?? new List<TimeEntry>();
            }
            catch
            {
                return new List<TimeEntry>();
            }
        }

        private void SaveTimeEntries(List<TimeEntry> timeEntries)
        {
            string fileName = GetFileName();

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

            if (highlightName)
            {
                detailsView.NameField.Focus();
                detailsView.NameField.SelectAll();
            }
        }

        #endregion

    }
}
