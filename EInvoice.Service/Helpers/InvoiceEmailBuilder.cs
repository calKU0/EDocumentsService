using EDocuments.Contracts.Models;

namespace EInvoice.Service.Helpers
{
    public static class InvoiceEmailBuilder
    {
        public static string BuildEInvoiceBodyForGroup(List<Invoice> invoices)
        {
            if (invoices == null || invoices.Count == 0)
                return string.Empty;

            var country = invoices[0].Country;
            var isPL = country == "PL";

            var greeting = isPL ? "Szanowny Kliencie," : "Dear Customer,";

            var invoiceNumbers = string.Join(", ", invoices.Select(i => i.Name));

            var invoiceInfo = isPL
                ? $"Przesyłamy w załączniku elektroniczne faktury:<br/><strong>{invoiceNumbers}</strong>"
                : $"Please find attached electronic invoices:<br/><strong>{invoiceNumbers}</strong>";

            var shipmentHeader = isPL
                ? "Dostawa / Śledzenie przesyłek"
                : "Delivery / Shipment Tracking";

            var shipmentLabelInvoice = isPL ? "Numery śledzenia" : "Tracking Numbers";
            var shipmentLabelLinks = isPL ? "Linki śledzenia" : "Tracking Links";

            var shipmentSection = "";

            foreach (var inv in invoices)
            {
                var numbersList = string.IsNullOrWhiteSpace(inv.TrackingNumbers)
                    ? new List<string>()
                    : inv.TrackingNumbers
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(n => n.Trim())
                        .ToList();

                var linksList = string.IsNullOrWhiteSpace(inv.TrackingLinks)
                    ? new List<string>()
                    : inv.TrackingLinks
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim())
                        .ToList();

                if (!numbersList.Any() && !linksList.Any())
                    continue;

                var numbersHtml = numbersList.Any()
                    ? $"<div style='{Styles.Label}'>{shipmentLabelInvoice}:</div>" +
                      $"<div style='{Styles.Value}'>{string.Join(", ", numbersList)}</div>"
                    : "";

                var linksHtml = linksList.Any()
                    ? $"<div style='{Styles.Label}'>{shipmentLabelLinks}:</div>" +
                      $"<div style='{Styles.Value}'>" +
                      string.Join("<br/>", linksList.Select(l =>
                          $"<a href='{l}' style='{Styles.Link}'>{l}</a>")) +
                      "</div>"
                    : "";

                shipmentSection += $@"
                <tr>
                    <td style='{Styles.Card}'>
                        <div style='{Styles.CardHeader}'>
                            {inv.Name}
                        </div>
                        {numbersHtml}
                        {linksHtml}
                    </td>
                </tr>";
            }

            var contact = isPL
                ? "W przypadku pytań prosimy o kontakt z opiekunem firmy, lub pod adresem: <a href='mailto:kontakt@gaska.com.pl' style='color:#1a73e8;text-decoration:none;'>kontakt@gaska.com.pl</a>"
                : "If you have any questions, contact us at: <a href='mailto:kontakt@gaska.com.pl' style='color:#1a73e8;text-decoration:none;'>kontakt@gaska.com.pl</a>";

            var signature = @"
                <div style='margin-top:20px;font-size:13px;color:#555;line-height:1.4;'>
                    <strong>Gąska sp. z o.o.</strong><br/>
                    Gotkowice 85<br/>
                    32-048 Jerzmanowice<br/>
                    POLAND<br/>
                    NIP: 677-000-03-35
                </div>";

            return $@"
                <div style='font-family:Arial, Helvetica, sans-serif; font-size:14px; color:#333; line-height:1.6; max-width:650px;margin:auto;padding:15px;background:#ffffff;'>

                    <p style='font-size:15px;'>{greeting}</p>

                    <div style='margin-top:15px; font-size:14px;'>
                        {invoiceInfo}
                    </div>

                    {(string.IsNullOrEmpty(shipmentSection) ? "" : $@"
                    <div style='margin-top:30px;'>
                        <div style='font-size:16px;font-weight:600;color:#111827;margin-bottom:12px;'>
                            {shipmentHeader}
                        </div>
                        <table width='100%' cellpadding='0' cellspacing='0' border='0'>
                            {shipmentSection}
                        </table>
                    </div>")}

                    <div style='margin-top:25px; font-size:14px;'>
                        {contact}
                    </div>

                    <div style='margin-top:25px;font-size:14px;'>
                        Pozdrawiam / Best Regards
                    </div>

                    {signature}

                </div>";
        }

        private static class Styles
        {
            public const string Card = @"
                padding:16px;
                border:1px solid #e5e7eb;
                border-radius:10px;
                background-color:#f9fafb;
                margin-bottom:12px;";

            public const string CardHeader = @"
                font-size:15px;
                font-weight:600;
                color:#111827;
                margin-bottom:10px;";

            public const string Label = @"
                font-size:12px;
                font-weight:600;
                color:#6b7280;
                margin-top:8px;";

            public const string Value = @"
                font-size:14px;
                color:#1f2937;
                margin-top:2px;
                line-height:1.5;";

            public const string Link = @"
                color:#2563eb;
                text-decoration:none;
                word-break:break-all;";
        }
    }
}
