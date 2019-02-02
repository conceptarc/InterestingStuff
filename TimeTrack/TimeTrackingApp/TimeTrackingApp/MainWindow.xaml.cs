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
using Hardcodet.Wpf;
using Hardcodet.Wpf.TaskbarNotification;

namespace TimeTrackingApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TaskbarIcon taskBarIcon;
        private ObservableCollection<TimeEntryView> viewCollection;
        private List<TimeEntry> currentTimeEntries;
        private Dictionary<int, TimeEntry> entryByIds;

        public MainWindow()
        {
            InitializeComponent();

            InitTaskbar();

            InitDataGrid();
        }

        // Set up the taskbar icon and its right-click menu option
        private void InitTaskbar()
        {
            ContextMenu rightClickMenu = new ContextMenu();
            MenuItem closeOption = new MenuItem { Header = "Close" };
            closeOption.Click += new RoutedEventHandler(
                (object sendr, RoutedEventArgs eventArgs) =>
                {
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
            currentTimeEntries = LoadTimeEntries();
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
        }

        private void RefreshViewCollection()
        {
            // Merge / update differences between the currentTimeEntries and viewCollection

            // Use for-int so we can modify the collection.
            for (int i = 0; i < viewCollection.Count; i++)
            {
                TimeEntryView view = viewCollection[i];
                int id = view.GetId();
                if (!entryByIds.ContainsKey(id))
                    continue;

                TimeEntryView newView = entryByIds[id].ToView(id);
                view.Active = newView.Active;
                view.Hours = newView.Hours;
                view.Name = newView.Name;
                view.Details = newView.Details;
            }

            // Insert new
            foreach (TimeEntry entry in currentTimeEntries)
            {
                if (entryByIds.ContainsValue(entry))
                    continue;

                int newId = entryByIds.Keys.Count + 1;
                entryByIds[newId] = entry;
                viewCollection.Add(entry.ToView(newId));
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

        private List<TimeEntry> LoadTimeEntries()
        {
#warning test data for now
            return new List<TimeEntry>
            {
                new TimeEntry
                {
                    Name = "test1",
                    Details = "developing this task and then logging my time into Jira.." +
                        ".\r\n- continuing after lunch",
                    TimePeriods = new List<TimePeriod>
                    {
                        new TimePeriod
                        {
                            StartTime = DateTime.Now.AddHours(-2.5),
                            EndTime = DateTime.Now.AddHours(-1.5)
                        }
                    }
                },
                new TimeEntry
                {
                    Name = "task2",
                    Details = "reading this textbook before committing the code.." +
                        ".\r\n- continuing after lunch",
                    TimePeriods = new List<TimePeriod>
                    {
                        new TimePeriod
                        {
                            StartTime = DateTime.Now.AddHours(-1.5)
                        }
                    }
                }
            };
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
        }

        private void CreateNewEntry()
        {
            TimeEntry activeEntry = currentTimeEntries.FirstOrDefault(m => m.IsActive);
            if (activeEntry != null)
                activeEntry.StopTimer();

            TimeEntry newEntry = new TimeEntry
            {
                Name = "test click",
                Details = "test test test"
            };
            currentTimeEntries.Add(newEntry);
            newEntry.StartTimer();
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
            TimeEntry activeEntry = currentTimeEntries.FirstOrDefault(m => m.IsActive);
            if (activeEntry != null)
                activeEntry.StopTimer();

            newEntry.StartTimer();
        }

        #endregion

    }
}
