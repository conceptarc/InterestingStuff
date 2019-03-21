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
using System.Threading;

namespace TimeTrackingApp
{
    /// <summary>
    /// Interaction logic for EntryDetails.xaml
    /// </summary>
    public partial class EntryDetails : Window
    {
        public MainWindow MainWindow { get; set; }
        public TimeEntry SelectedTimeEntry { get; set; }
        private EditTime popup;

        public EntryDetails()
        {
            InitializeComponent();

            // update the time on this window

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000);

                    this.Dispatcher.Invoke(() =>
                    {
                        if (this.Visibility != Visibility.Visible)
                            return; // don't update if this window is hidden

                        if (CurrentTime.IsSelectionActive)
                            return; // don't update as the user is typing

                        if (SelectedTimeEntry == null)
                            return; // (potential) null exception

                        CurrentTime.Text = SelectedTimeEntry.ToView(0).Hours;
                    });
                }
            });
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;

            CloseWithoutSave();
        }

        private void SaveAndClose()
        {
            // save the name, details / description, and time
            SelectedTimeEntry.Name = NameField.Text;
            SelectedTimeEntry.Details = DetailsField.Text;

            decimal currentHours = decimal.Parse(SelectedTimeEntry.ToView(0).Hours);
            decimal newHours = decimal.Parse(CurrentTime.Text);
            SelectedTimeEntry.OffsetHours += newHours - currentHours;

            MainWindow.SaveImmediately();

            this.Hide();

            MainWindow.Show();
            MainWindow.Focus();
        }

        private void DeleteEntry()
        {
            var confirmResult = MessageBox.Show("Confirm delete.", "Delete Confirmation", 
                MessageBoxButton.OKCancel);

            if (confirmResult == MessageBoxResult.Cancel)
            {
                return;
            }

            MainWindow.CurrentTimeEntries.Remove(SelectedTimeEntry);
            MainWindow.SaveImmediately();

            this.Hide();

            MainWindow.Show();
            MainWindow.Focus();
        }

        public bool CloseWithoutSave()
        {
            if (SelectedTimeEntry == null)
                return true;

            bool changeDetected = SelectedTimeEntry.Name != NameField.Text ||
                SelectedTimeEntry.Details != DetailsField.Text;

            if (changeDetected)
            {
                var confirmResult = MessageBox.Show("Close without saving?", "Close Confirmation", 
                    MessageBoxButton.OKCancel);

                if (confirmResult == MessageBoxResult.Cancel)
                {
                    return false;
                }
            }

            this.Hide();
            if (this.popup != null)
            {
                this.popup.Close();
                this.popup = null;
            }

            MainWindow.Show();
            MainWindow.Focus();
            return true;
        }

        private void WindowScreen_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1) // save and then close
                SaveAndClose();

            if (e.Key == Key.F4) // close without saving
                CloseWithoutSave();

            if (e.Key == Key.F9) // delete
                DeleteEntry();
        }

        private void SaveClose_Click(object sender, RoutedEventArgs e)
        {
            SaveAndClose();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            DeleteEntry();
        }

        private void CancelClose_Click(object sender, RoutedEventArgs e)
        {
            CloseWithoutSave();
        }

        private void AddTime_Click(object sender, RoutedEventArgs e)
        {
            // Ironically I don't have time to think of a proper solution for adjusting
            // the time of an entry.
            // Since the total duration is calculated each time, we can use a variable offset.

            this.popup = new EditTime();
            popup.ParentWindow = this;
            popup.SelectedTimeEntry = SelectedTimeEntry;
            popup.NewHours.Text = SelectedTimeEntry.ToView(0).Hours;
            popup.Show();
            popup.Focus();
            popup.NewHours.Focus();
            popup.NewHours.SelectAll();
        }
    }
}
