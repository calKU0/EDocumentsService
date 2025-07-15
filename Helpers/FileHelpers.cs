using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFakturyService.Helpers
{
    public static class FileHelpers
    {
        public static void BackupFiles(string backupPath)
        {
            string sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "faktury");

            try
            {
                if (Directory.Exists(sourcePath))
                {
                    if (!Directory.Exists(backupPath))
                    {
                        Directory.CreateDirectory(backupPath);
                    }

                    string[] files = Directory.GetFiles(sourcePath);

                    string fileName = "";
                    string destFile = "";

                    foreach (string s in files)
                    {
                        fileName = Path.GetFileName(s);
                        destFile = Path.Combine(backupPath, fileName);
                        File.Copy(s, destFile, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Błąd backupowaniu plików.");
            }
        }
    }
}