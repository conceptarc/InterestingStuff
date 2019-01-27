using System;
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

        public MainWindow()
        {
            InitializeComponent();

            #region Setup the taskbar icon and its right-click menu option.

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
                    this.Show(); // same as TrayMouseDoubleClick
                });

            #endregion
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;

            this.Hide();

            taskBarIcon.ShowBalloonTip("Time Tracker App",
                "Minimized to task tray. Click here to undo.", BalloonIcon.Info);
        }
    }
}
