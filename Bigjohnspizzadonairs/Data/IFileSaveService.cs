using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bigjohnspizzadonairs.Data
{
    public interface IFileSaveService
    {
        Task<string> SaveFileAsync(byte[] fileContents, string defaultFileName, string fileTypeFilter);
    }
}
