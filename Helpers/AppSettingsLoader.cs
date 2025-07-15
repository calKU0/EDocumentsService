using EFakturyService.Models.Settings;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFakturyService.Helpers
{
    public static class AppSettingsLoader
    {
        public static SmtpSettings LoadSmtpSettings()
        {
            return new SmtpSettings
            {
                Name = GetString("SMTP"),
                Port = GetInt("SMTPPort", 0),
                Login = GetString("SMTPLogin"),
                Password = GetString("SMTPHasło"),
            };
        }

        public static XlSettings LoadXlSettings()
        {
            return new XlSettings
            {
                ApiVersion = GetInt("XLApiVersion", 20241),
                ProgramName = GetString("XLProgramName"),
                Login = GetString("XLLogin"),
                Password = GetString("XLHasło"),
                Database = GetString("XLBaza"),
            };
        }

        public static AppSettings LoadAppSettings()
        {
            return new AppSettings
            {
                ServiceStartHour = GetInt("GodzinaWysyłki", 23),
                LogsExpirationDate = GetInt("UsuwaćLogiStarszeNiżXDni", 14),
                BackupPath = GetString("BackupPath"),
            };
        }

        private static string GetString(string key, bool required = true)
        {
            var value = ConfigurationManager.AppSettings[key];

            if (required && string.IsNullOrWhiteSpace(value))
                throw new ConfigurationErrorsException($"Missing required appSetting: '{key}'");

            return value;
        }

        private static int GetInt(string key, int defaultValue)
        {
            var raw = ConfigurationManager.AppSettings[key];
            if (int.TryParse(raw, out int result))
                return result;

            return defaultValue;
        }
    }
}