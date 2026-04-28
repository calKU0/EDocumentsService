using cdn_api;
using EDocuments.Contracts.Services;
using EDocuments.Contracts.Settings;
using Microsoft.Extensions.Options;
using System.Runtime.InteropServices;

namespace EDocuments.Infrastructure.Services
{
    public class XlApiService : IXlApiService
    {
        [DllImport("ClaRUN.dll")]
        public static extern void AttachThreadToClarion(int _flag);

        private readonly XlApiSettings _xlSettings;
        public XlApiService(IOptions<XlApiSettings> xlSettings)
        {
            _xlSettings = xlSettings.Value;
        }

        public int Login()
        {
            int sessionId = 0;
            AttachThreadToClarion(1);
            XLLoginInfo_20251 xLLoginInfo = new XLLoginInfo_20251
            {
                Wersja = _xlSettings.ApiVersion,
                ProgramID = _xlSettings.ProgramName,
                Baza = _xlSettings.Database,
                OpeIdent = _xlSettings.Login,
                OpeHaslo = _xlSettings.Password,
                TrybWsadowy = 1
            };

            int result = cdn_api.cdn_api.XLLogin(xLLoginInfo, ref sessionId);
            if (result != 0)
                throw new InvalidOperationException($"Error while loggin in. Error code: {result}");

            return sessionId;
        }

        public void Logout(int sessionId)
        {
            AttachThreadToClarion(1);
            XLLogoutInfo_20251 xLLogoutInfo = new XLLogoutInfo_20251
            {
                Wersja = _xlSettings.ApiVersion,
            };

            int result = cdn_api.cdn_api.XLLogout(sessionId);
            if (result != 0)
                throw new InvalidOperationException($"Error while loggin out. Error code: {result}");
        }

        public void GeneratePrint(XlPrintSettings printSetting, string path, string? filtrSql)
        {
            AttachThreadToClarion(1);
            var xlPrint = new XLWydrukInfo_20251
            {
                Wersja = _xlSettings.ApiVersion,
                Zrodlo = printSetting.PrintSource,
                Wydruk = printSetting.PrintId,
                Format = printSetting.PrintFormat,
                FiltrSQL = filtrSql,
                Urzadzenie = 2,
                DrukujDoPliku = 1,
                PlikDocelowy = path
            };

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var result = cdn_api.cdn_api.XLWykonajPodanyWydruk(xlPrint);
            if (result != 0)
                throw new Exception(CheckXlError(87, result));
        }

        private string CheckXlError(int function, int errorCode)
        {
            //AttachThreadToClarion(1);
            string errorMessage = "";

            XLKomunikatInfo_20251 komunikatInfo = new XLKomunikatInfo_20251();
            komunikatInfo.Wersja = _xlSettings.ApiVersion;
            komunikatInfo.Funkcja = function;
            komunikatInfo.Blad = errorCode;
            komunikatInfo.Tryb = 0;

            int result = cdn_api.cdn_api.XLOpisBledu(komunikatInfo);

            if (result == 0)
            {
                errorMessage = komunikatInfo.OpisBledu;
                return errorMessage;
            }
            else
            {
                return $"Error while checking error. Code: {errorMessage}";
            }
        }
    }
}
