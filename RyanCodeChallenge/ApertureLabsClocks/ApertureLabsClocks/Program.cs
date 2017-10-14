using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Globalization;

namespace ApertureLabsClocks
{
    class Employee
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    class DayPeriods
    {
        // I have to make these properties the same as the sample file output.
        public decimal period1 { get; set; }
        public decimal period2 { get; set; }
        public decimal period3 { get; set; }
        public decimal period4 { get; set; }
    }

    class ShiftByDay
    {
        // I have to make these properties the same as the sample file output.
        public string date { get; set; }
        public decimal total { get; set; }
        public DayPeriods labour_by_time_period { get; set; }
    }

    class EmployeeInfo
    {
        // I have to make these properties the same as the sample file output.
        public int employee_id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public List<ShiftByDay> labour { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string filePath = "..\\..\\apertureLabsClocks.json";
            if (!File.Exists(filePath))
            {
                Console.Out.WriteLine("Put this in the same location as 'apertureLabsClocks.json'.");
                Console.Out.WriteLine("Enter any key to exit.");
                Console.In.Read();
                return;
            }

            // Parse this file.
            string fileText = File.ReadAllText(filePath);
            JObject jObj = (JObject)JsonConvert.DeserializeObject(fileText);

            // Collect employee info.
            List<Employee> employeeList = new List<Employee>();
            foreach(JObject employee in jObj["employees"])
            {
                employeeList.Add(new Employee()
                {
                    Id = Convert.ToInt32(employee["id"].ToString()),
                    FirstName = employee["first_name"].ToString(),
                    LastName = employee["last_name"].ToString()
                });
            }

            // Iterate through the clock-in/outs and accumulate outputs.
            List<EmployeeInfo> shiftList = new List<EmployeeInfo>();
            foreach (JObject clock in jObj["clocks"])
            {
                // Parse the JSON data.
                int id = Convert.ToInt32(clock["employee_id"].ToString());
                DateTime clockIn = DateTime.Parse(clock["clock_in_datetime"].ToString());
                DateTime clockOut = DateTime.Parse(clock["clock_out_datetime"].ToString());

                // Try to find existing employee info.
                EmployeeInfo employee = shiftList.Where(m => m.employee_id == id).FirstOrDefault();
                if (employee == null)
                {
                    // If not found, we need to create a new record.
                    employee = new EmployeeInfo();
                    employee.employee_id = id;

                    // Find employee first & last names.
                    Employee emp = employeeList.Where(m => m.Id == id).FirstOrDefault();
                    if (emp == null)
                    {
                        Console.Out.WriteLine("Employee [" + id + "] is not found.");
                        continue;
                    }
                    employee.first_name = emp.FirstName;
                    employee.last_name = emp.LastName;

                    employee.labour = new List<ShiftByDay>();
                    shiftList.Add(employee);
                }

                List<ShiftByDay> currentShiftInfo = GetShiftsByDay(clockIn, clockOut);
                employee.labour = ConsolidateShifts(employee.labour, currentShiftInfo);
            }

            string jsonOutput = JsonConvert.SerializeObject(shiftList, Formatting.Indented);
            string outputFileName = "EmployeeShiftData.json";
            File.WriteAllText(outputFileName, jsonOutput);

            Console.Out.WriteLine("Output: " + outputFileName);
            Console.Out.WriteLine("Enter any key to exit.");
            Console.In.Read();
        }

        static List<ShiftByDay> GetShiftsByDay(DateTime start, DateTime end)
        {
            List<ShiftByDay> outputList = new List<ShiftByDay>();

            // We need to count from the day before because Period 4 can go until 5 AM.
            DateTime currentTime = new DateTime(start.Year, start.Month, start.Day, 5, 0, 0).AddDays(-1);
            while (DateTime.Compare(currentTime, end) < 0) // currentDate < end
            {
                ShiftByDay shift = new ShiftByDay();
                shift.date = currentTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                // Time to calculate the periods.
                DayPeriods day = new DayPeriods();
                
                day.period1 = CalculateNextPeriod(ref currentTime, start, end, 7); //  5 am + 7 = 12 pm
                day.period2 = CalculateNextPeriod(ref currentTime, start, end, 6); // 12 pm + 6 =  6 pm
                day.period3 = CalculateNextPeriod(ref currentTime, start, end, 5); //  6 pm + 5 = 11 pm
                day.period4 = CalculateNextPeriod(ref currentTime, start, end, 6); // 11 pm + 6 =  5 am

                // If this day has any hours, then add to our result set.
                decimal total = day.period1 + day.period2 + day.period3 + day.period4;
                if (total > 0)
                {
                    shift.labour_by_time_period = day;
                    shift.total = total;
                    outputList.Add(shift);
                }
            }

            return outputList;
        }

        static decimal CalculateNextPeriod(ref DateTime currentTime, DateTime startTime, DateTime endTime, double hourIncrement)
        {
            double hoursFromStart = startTime.Subtract(currentTime.AddHours(hourIncrement)).TotalHours;
            if (hoursFromStart > 0)
            {
                // Here we have not reached the start time yet.
                currentTime = currentTime.AddHours(hourIncrement);
                return 0;
            }

            double hoursFromEnd = endTime.Subtract(currentTime.AddHours(hourIncrement)).TotalHours;
            // the max/min is for upper and lower bounds
            decimal periodHours = Convert.ToDecimal(Math.Max(Math.Min(hoursFromEnd, hourIncrement), 0));
            currentTime = currentTime.AddHours(hourIncrement);

            return Math.Round(periodHours, 1); // max 1 decimal place
        }

        static List<ShiftByDay> ConsolidateShifts(List<ShiftByDay> left, List<ShiftByDay> right)
        {
            List<ShiftByDay> finalList = new List<ShiftByDay>();

            // Create the union of both left and right sets.

            foreach(ShiftByDay day in right)
            {
                ShiftByDay existingDay = finalList.Where(m => m.date == day.date).FirstOrDefault();
                if (existingDay == null) // No merge required - just add the right entry.
                {
                    finalList.Add(day);
                    continue;
                }

                // Okay this will fail if the same check-in and
                // check-out times show up again as duplicate records.
                // I.e. I assume the clock times are unique per employee.
                existingDay.total += day.total;
                existingDay.labour_by_time_period.period1 += day.labour_by_time_period.period1;
                existingDay.labour_by_time_period.period2 += day.labour_by_time_period.period2;
                existingDay.labour_by_time_period.period3 += day.labour_by_time_period.period3;
                existingDay.labour_by_time_period.period4 += day.labour_by_time_period.period4;
            }

            return finalList.OrderBy(m => m.date).ToList();
        }
    }
}
