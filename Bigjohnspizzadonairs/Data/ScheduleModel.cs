using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bigjohnspizzadonairs.Data
{
        public class ScheduleModel
        {
            public int EmployeeId { get; set; }
            public DateTime ScheduleDate { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
        }

    }
