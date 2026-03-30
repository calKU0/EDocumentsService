using EDocuments.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace EDocuments.Infrastructure.Services
{
    public class FileService : IFileService
    {
        private readonly ILogger<FileService> _logger;
        public FileService(ILogger<FileService> logger)
        {
            _logger = logger;
        }
        public void BackupFiles(string sourceFolder, string destinationFolder)
        {

            if (Directory.Exists(sourceFolder))
            {
                Directory.CreateDirectory(destinationFolder);

                string[] files = Directory.GetFiles(sourceFolder);

                foreach (string file in files)
                {
                    try
                    {
                        DateTime lastModified = File.GetLastWriteTime(file);

                        if (lastModified >= DateTime.Now.AddDays(-2))
                        {
                            string fileName = Path.GetFileName(file);
                            string destFile = Path.Combine(destinationFolder, fileName);
                            File.Copy(file, destFile, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error backing up file: {file}");
                    }
                }
            }
        }
    }
}
