using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bigjohnspizzadonairs.Data
{
    public class ShiftDetail
    {
        public int ShiftId { get; set; }
        public int EmployeeId { get; set; }

        public string EmployeeName { get; set; }
        public DateTime ShiftDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public DateTime StartDateTime => ShiftDate.Add(StartTime);
        public DateTime EndDateTime => ShiftDate.Add(EndTime);
    }

}
