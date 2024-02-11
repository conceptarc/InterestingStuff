using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TimeTrackingApp
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private bool gui;
        private DateTime currentViewingDate;

        public LoginWindow(DateTime currentViewingDate, bool gui)
        {
            InitializeComponent();
            UsernameTxt.Focus();
            this.gui = gui;
            this.currentViewingDate = currentViewingDate;
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            TimeSubmission.DoSubmission(UsernameTxt.Text, PasswordTxt.Password, currentViewingDate, gui);
            Close();
        }

    }
}
