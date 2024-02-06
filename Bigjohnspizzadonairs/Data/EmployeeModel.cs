using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bigjohnspizzadonairs.Data
{
    public class EmployeeModel
    {
        public int EmployeeId{ get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int Age { get; set; }
        public string Position { get; set; }
        public string ContactNumber { get; set; }
        public string EmergencyContactNumber { get; set; }
        public string Password { get; set; }
    }
}
