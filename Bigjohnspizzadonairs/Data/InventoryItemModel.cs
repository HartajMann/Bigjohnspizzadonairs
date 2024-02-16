using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bigjohnspizzadonairs.Data
{
    public class InventoryItemModel
    {

        public int InventoryId { get; set; }
        public string Branch { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime Timestamp { get; set; }

    }
}
