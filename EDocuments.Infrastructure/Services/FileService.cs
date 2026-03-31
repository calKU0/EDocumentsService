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

        public void DeleteFilesFromFolder(string folder, string? extension = null)
        {
            if (!Directory.Exists(folder))
            {
                _logger.LogError($"Can't deletes file from {folder}. Folder does not exist");
                return;
            }

            string[] files = Directory.GetFiles(folder);
            string[] filesWithExtension = string.IsNullOrEmpty(extension) ? files : files.Where(f => Path.GetExtension(f) == extension).ToArray();

            foreach (string file in filesWithExtension)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error deleting file: {file}");
                }
            }
        }
    }
}
