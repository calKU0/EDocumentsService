namespace EDocuments.Contracts.Services
{
    public interface IFileService
    {
        public void BackupFiles(string sourceFolder, string destinationFolder);
        public void DeleteFilesFromFolder(string folder, string? extension = null);
    }
}
