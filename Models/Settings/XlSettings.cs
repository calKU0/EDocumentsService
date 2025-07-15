using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFakturyService.Models.Settings
{
    public class XlSettings
    {
        public int ApiVersion { get; set; }
        public string ProgramName { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Database { get; set; }
    }
}