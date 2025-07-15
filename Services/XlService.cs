using cdn_api;
using EFakturyService.Models;
using EFakturyService.Models.Settings;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EFakturyService.Services
{
    public class XlService : IXlService
    {
        [DllImport("ClaRUN.dll")]
        public static extern void AttachThreadToClarion(int _flag);

        private readonly XlSettings _xlSettings;
        private int _sessionId;

        public XlService(XlSettings xlSettings)
        {
            _xlSettings = xlSettings;
        }

        public string GenerateInvoicePdf(InvoiceDto invoice)
        {
            string pdfPath = string.Empty;

            var printMap = new Dictionary<(int GidType, string Country), (int Wydruk, int Format)>
            {
                { (2033, "PL"), (invoice.NumberOfStapledWZDocuments == 0 ? 135 : 173, 1) },
                { (2041, "PL"), (138, 1) },
                { (2037, "PL"), (136, 1) },
                { (2045, "PL"), (137, 1) },

                { (2033, "FOREIGN"), (135, 1) },
                { (2041, "FOREIGN"), (138, 1) },
                { (2037, "FOREIGN"), (136, 2) },
                { (2045, "FOREIGN"), (137, 2) },
            };

            var isForeign = invoice.Country != "PL";
            var countryKey = isForeign ? "FOREIGN" : "PL";

            if (printMap.TryGetValue((invoice.GidType, countryKey), out var settings))
            {
                var wydruk = settings.Wydruk;
                var format = settings.Format;

                var filtrSQL = $"(TrN_GIDTyp={invoice.GidType} AND TrN_GIDNumer={invoice.GidNumber})";
                var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "faktury");
                Directory.CreateDirectory(directory);
                var filePath = Path.Combine(directory, invoice.FileName + ".pdf");

                var result = ExecuteXLPrint(wydruk, format, filtrSQL, filePath);

                if (result != 0)
                    throw new InvalidOperationException($"XL drukowanie błąd. Kod błędu: {result} ({CheckXlError(87, result)})");
                else
                    pdfPath = filePath;
            }
            else
            {
                throw new InvalidOperationException($"Nie obsłużony GIDTyp dokumentu: {invoice.GidType} dla kraju: {invoice.Country}");
            }

            return pdfPath;
        }

        public string GenerateExportDeclarationPdf(InvoiceDto invoice)
        {
            string pdfPath = string.Empty;

            var printMap = new Dictionary<string, (int Wydruk, int Format)>
            {
                { "RO", (155, 2) },
                { "FOREIGN", (155, 1) },
            };

            var isOther = invoice.Country != "RO";
            var countryKey = isOther ? "FOREIGN" : "RO";

            if (printMap.TryGetValue((countryKey), out var settings))
            {
                var wydruk = settings.Wydruk;
                var format = settings.Format;

                var filtrSQL = $"(Knt_GIDTyp=32 AND Knt_GIDFirma=449892 AND Knt_GIDNumer={invoice.ClientId})";
                var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "oświadczenia");
                Directory.CreateDirectory(directory);
                var filePath = Path.Combine(directory, invoice.ClientName + "_declaration_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".pdf");

                var result = ExecuteXLPrint(wydruk, format, filtrSQL, filePath);

                if (result != 0)
                    throw new InvalidOperationException($"XL drukowanie błąd.Kod błędu: {result}({CheckXlError(87, result)})");
                else
                    pdfPath = filePath;
            }
            else
            {
                throw new InvalidOperationException($"Nie obsłużony GIDTyp dokumentu: {invoice.GidType} dla kraju: {invoice.Country}");
            }

            return pdfPath;
        }

        public bool Login()
        {
            XLLoginInfo_20241 xLLoginInfo = new XLLoginInfo_20241
            {
                Wersja = _xlSettings.ApiVersion,
                ProgramID = _xlSettings.ProgramName,
                Baza = _xlSettings.Database,
                OpeIdent = _xlSettings.Login,
                OpeHaslo = _xlSettings.Password,
                TrybWsadowy = 1
            };

            int result = cdn_api.cdn_api.XLLogin(xLLoginInfo, ref _sessionId);
            if (result != 0)
            {
                throw new InvalidOperationException($"Błąd logowania XL. Kod błędu: {result}");
            }

            return true;
        }

        public bool Logout()
        {
            AttachThreadToClarion(1);
            XLLogoutInfo_20241 xLLogoutInfo = new XLLogoutInfo_20241
            {
                Wersja = _xlSettings.ApiVersion,
            };

            int result = cdn_api.cdn_api.XLLogout(_sessionId);
            if (result != 0)
            {
                throw new InvalidOperationException($"Błąd wylogowania XL. Kod błędu: {result}");
            }

            return true;
        }

        private int ExecuteXLPrint(int wydruk, int format, string filtrSQL, string targetFile)
        {
            AttachThreadToClarion(1);
            var xlPrint = new XLWydrukInfo_20241
            {
                Wersja = _xlSettings.ApiVersion,
                Zrodlo = 1,
                Wydruk = wydruk,
                Format = format,
                FiltrSQL = filtrSQL,
                Urzadzenie = 2,
                DrukujDoPliku = 1,
                PlikDocelowy = targetFile
            };

            return cdn_api.cdn_api.XLWykonajPodanyWydruk(xlPrint);
        }

        private string CheckXlError(int funkcja, int numerBledu)
        {
            //AttachThreadToClarion(1);
            string komunikat = "";

            try
            {
                XLKomunikatInfo_20241 komunikatInfo = new XLKomunikatInfo_20241();
                komunikatInfo.Wersja = _xlSettings.ApiVersion;
                komunikatInfo.Funkcja = funkcja;
                komunikatInfo.Blad = numerBledu;
                komunikatInfo.Tryb = 0;

                int wynik_XLOpisBledu = cdn_api.cdn_api.XLOpisBledu(komunikatInfo);

                if (wynik_XLOpisBledu == 0)
                {
                    komunikat = komunikatInfo.OpisBledu;
                    return komunikat;
                }
                else
                {
                    return "Błąd pobierania komunikatu";
                }
            }
            catch (Exception ex)
            {
                return "Błąd pobierania komunikatu " + ex.ToString();
            }
        }
    }
}