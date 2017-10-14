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

        static List<ShiftByDay> GetShiftsByDay(DateTime clockIn, DateTime clockOut)
        {
            List<ShiftByDay> outputList = new List<ShiftByDay>();

            // We need to count from the day before because Period 4 can go until 5 AM.
            DateTime currentTime = new DateTime(clockIn.Year, clockIn.Month, clockIn.Day, 5, 0, 0).AddDays(-1);
            while (DateTime.Compare(currentTime, clockOut) < 0) // currentDate < end
            {
                ShiftByDay shift = new ShiftByDay();
                shift.date = currentTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                // Time to calculate the periods.
                DayPeriods day = new DayPeriods();
                
                day.period1 = CalculateNextPeriod(ref currentTime, clockIn, clockOut, 7); //  5 am + 7 = 12 pm
                day.period2 = CalculateNextPeriod(ref currentTime, clockIn, clockOut, 6); // 12 pm + 6 =  6 pm
                day.period3 = CalculateNextPeriod(ref currentTime, clockIn, clockOut, 5); //  6 pm + 5 = 11 pm
                day.period4 = CalculateNextPeriod(ref currentTime, clockIn, clockOut, 6); // 11 pm + 6 =  5 am

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

        static decimal CalculateNextPeriod(ref DateTime currentTime, DateTime clockIn, DateTime clockOut, double hourIncrement)
        {
            DateTime periodStart = currentTime;
            DateTime periodEnd = currentTime.AddHours(hourIncrement);

            /* Visual representation of combinations:
             * s = period start
             * e = period end
             * [ = clock in
             * ] = clock out
             * _ = time counted
             * 
             * 1) s e [ ]   no hours
             * 2) s [_e ]   period end - clock in           If the current period straddles the end time.
             * 3) s [_] e   clock out - clock in            If the start and end times are both within this period.
             * 4) [ s_] e   clock out - period start        If the current period straddles the start time.
             * 5) [ s_e ]   period end - period start       All the hours in this period are clocked.
             * 6) [ ] s e   no hours
             * 
             * We can see that the upper bound is the min(period end, clock out).
             * Also the lower bound is the max(period start, clock in).
            */

            DateTime end = Min(periodEnd, clockOut);
            DateTime start = Max(periodStart, clockIn);

            double workHours = Math.Max(0, end.Subtract(start).TotalHours);
            
            currentTime = periodEnd; /// increment time
            return Math.Round(Convert.ToDecimal(workHours), 1); // round to 1 decimal place
        }

        static DateTime Min(DateTime d1, DateTime d2)
        {
            return DateTime.Compare(d1, d2) > 0 ? d2 : d1; // d1 is later
        }

        static DateTime Max(DateTime d1, DateTime d2)
        {
            return DateTime.Compare(d1, d2) > 0 ? d1 : d2; // d1 is later
        }

        static List<ShiftByDay> ConsolidateShifts(List<ShiftByDay> left, List<ShiftByDay> right)
        {
            List<ShiftByDay> finalList = new List<ShiftByDay>();

            // Create the union of both left and right sets.

            foreach (ShiftByDay day in left)
            {
                finalList.Add(day); // Start with the left list.
            }

            foreach (ShiftByDay day in right)
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
