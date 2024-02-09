using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bigjohnspizzadonairs.Data
{
    public class AvailabilityModel
    {
       
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string Name { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
    }

}
