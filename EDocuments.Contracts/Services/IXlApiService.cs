using EDocuments.Contracts.Settings;

namespace EDocuments.Contracts.Services
{
    public interface IXlApiService
    {
        public void Login();

        public void Logout();

        public void GeneratePrint(XlPrintSettings printSettings, string path, string? filtrSql);
    }
}