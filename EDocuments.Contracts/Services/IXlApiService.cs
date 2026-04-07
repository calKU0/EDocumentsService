using EDocuments.Contracts.Settings;

namespace EDocuments.Contracts.Services
{
    public interface IXlApiService
    {
        public int Login();

        public void Logout(int sessionId);

        public void GeneratePrint(XlPrintSettings printSettings, string path, string? filtrSql);
    }
}