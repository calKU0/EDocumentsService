using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFakturyService.Models.Settings
{
    public class AppSettings
    {
        public int ServiceStartHour { get; set; }
        public int LogsExpirationDate { get; set; }
        public string BackupPath { get; set; }
    }
}