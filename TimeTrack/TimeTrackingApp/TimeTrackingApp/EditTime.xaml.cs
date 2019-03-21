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
using System.Windows.Shapes;

namespace TimeTrackingApp
{
    /// <summary>
    /// Interaction logic for EditTime.xaml
    /// </summary>
    public partial class EditTime : Window
    {
        public TimeEntry SelectedTimeEntry { get; set; }
        public EntryDetails ParentWindow { get; set; }

        public EditTime()
        {
            InitializeComponent();
        }

        private void WindowScreen_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) // save and then close
                ApplyNewTime();
        }

        private void ApplyNewTime()
        {
            decimal currentHours = decimal.Parse(SelectedTimeEntry.ToView(0).Hours);
            if (!decimal.TryParse(NewHours.Text, out decimal newTimeInHours))
            {
                NewHours.Text = currentHours.ToString();
                return;
            }

            // careful not to accidentally change the source data
            // i.e. still allow the user to cancel later
            ParentWindow.CurrentTime.Text = newTimeInHours.ToString();
            this.Close();
        }

        private void SubmitDelta_Click(object sender, RoutedEventArgs e)
        {
            ApplyNewTime();
        }
    }
}
