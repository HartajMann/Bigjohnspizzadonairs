using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bigjohnspizzadonairs.Data
{
    public class ScheduleDisplayModel
    {
        public int ScheduleId { get; set; }
        public string EmployeeName { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

}
