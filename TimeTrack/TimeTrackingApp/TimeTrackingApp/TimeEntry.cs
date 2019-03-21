using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace TimeTrackingApp
{
    public class TimeEntry
    {
        public Guid UniqueId { get; set; }
        public string Name { get; set; }
        public string Details { get; set; }

        private List<TimePeriod> timePeriods;
        public List<TimePeriod> TimePeriods
        {
            get
            {
                if (timePeriods == null)
                    timePeriods = new List<TimePeriod>();
                return timePeriods;
            }
            set
            {
                timePeriods = value;
            }
        }
        public decimal OffsetHours { get; set; }

        public bool IsActive
        {
            get
            {
                TimePeriod lastPeriod = TimePeriods.LastOrDefault();
                return lastPeriod != null && !lastPeriod.EndTime.HasValue;
            }
        }

        public decimal DurationHours
        {
            get
            {
                decimal totalHours = Convert.ToDecimal(this.GetDuration().TotalHours);
                // don't deal with double imprecision
                return Math.Round(totalHours, 4) + OffsetHours;
            }
        }

        public TimeSpan GetDuration()
        {
            TimeSpan duration = new TimeSpan();

            foreach (TimePeriod period in TimePeriods)
            {
                DateTime endTime = period.EndTime ?? DateTime.Now;
                TimeSpan currentDuration = endTime.Subtract(period.StartTime);

                duration = duration.Add(currentDuration);
            }

            return duration;
        }

        //public decimal GetDurationHours(int decimalPrecision = 2)
        //{
        //    decimal totalHours = Convert.ToDecimal(this.GetDuration().TotalHours);
        //    // don't deal with double imprecision
        //    return Math.Round(totalHours, decimalPrecision);
        //}

        public void StartTimer()
        {
            // Check if the last time period is still running. If so do nothing.
            TimePeriod lastPeriod = TimePeriods.LastOrDefault();
            if (lastPeriod != null && !lastPeriod.EndTime.HasValue)
                return;

            // Otherwise add a new timing period.
            TimePeriods.Add(new TimePeriod { StartTime = DateTime.Now });
        }

        public void StopTimer()
        {
            // Close the last time period if existing.
            TimePeriod lastPeriod = TimePeriods.LastOrDefault();
            if (lastPeriod != null && !lastPeriod.EndTime.HasValue)
                lastPeriod.EndTime = DateTime.Now;
        }

        public TimeEntryView ToView(int id)
        {
            var view = new TimeEntryView
            {
                Active = IsActive,
                Hours = DurationHours.ToString("0.0000"),
                Name = Name,
                Details = Details
            };
            view.SetId(id);

            return view;
        }
    }

    public class TimePeriod
    {
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    // This class is for data grid binding & display.
    public class TimeEntryView : INotifyPropertyChanged
    {
        private int Id;

        private bool active;
        public bool Active
        {
            get { return active; }
            set
            {
                active = value;
                OnPropertyChanged("Active");
            }
        }

        private string hours;
        public string Hours
        {
            get { return hours; }
            set
            {
                hours = value;
                OnPropertyChanged("Hours");
            }
        }

        private string name;
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
        }

        private string details;
        public string Details
        {
            get { return details; }
            set
            {
                details = value;
                OnPropertyChanged("Details");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void SetId(int id)
        {
            Id = id;
        }

        public int GetId()
        {
            return Id;
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
