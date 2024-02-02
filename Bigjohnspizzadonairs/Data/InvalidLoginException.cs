using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bigjohnspizzadonairs.Data
{
    public class InvalidLoginException:Exception
    {
        public InvalidLoginException() :base("Entered Username or Password is Incorrect."){ }

        public InvalidLoginException(string message) : base(message) { }

    }
}
