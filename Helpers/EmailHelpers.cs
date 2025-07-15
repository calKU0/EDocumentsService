using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EFakturyService.Helpers
{
    public static class EmailHelpers
    {
        private static bool IsValidEmail(string email)
        {
            string regex1 = @"^[^@\s]+@[^@\s]+\.[a-zA-Z0-9]{2,}.[a-zA-Z]{2,}$";
            string regex2 = @"^[^@\s]+@[^@\s]+\.[a-zA-Z]{2,}$";

            return Regex.IsMatch(email, regex1, RegexOptions.IgnoreCase) || Regex.IsMatch(email, regex2, RegexOptions.IgnoreCase);
        }
    }
}