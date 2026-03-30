using EDocuments.Contracts.Models;

namespace EInvoice.Service.Helpers
{
    public static class EmailHelpers
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

            var shipmentLabelInvoice = isPL ? "Faktura" : "Invoice";
            var shipmentLabelLinks = isPL ? "Linki śledzenia" : "Tracking Links";

            var shipmentSection = "";

            foreach (var inv in invoices)
            {
                var numbers = string.IsNullOrWhiteSpace(inv.TrackingNumbers) ? "" :
                    string.Join("<br/>",
                        inv.TrackingNumbers.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(n => n.Trim()));

                var links = string.IsNullOrWhiteSpace(inv.TrackingLinks) ? "" :
                    string.Join("<br/>",
                        inv.TrackingLinks.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                         .Select(l => $"<a href='{l.Trim()}' style='color:#1a73e8;text-decoration:none;'>{l.Trim()}</a>"));

                if (string.IsNullOrEmpty(numbers) && string.IsNullOrEmpty(links))
                    continue;

                shipmentSection += $@"
                <div style='margin-top:15px;padding:15px;border-radius:8px;background:#f7f9fc;border:1px solid #dce3eb;box-shadow:0 2px 4px rgba(0,0,0,0.05);'>
                    <div style='font-weight:bold;font-size:15px;color:#222;margin-bottom:8px;'>{shipmentLabelInvoice}: {inv.Name}</div>
                    {(string.IsNullOrEmpty(numbers) ? "" : $"<div style='font-size:14px;color:#333;margin-bottom:6px;'>{numbers.Replace("|", ", ")}</div>")}
                    {(string.IsNullOrEmpty(links) ? "" : $"<div style='font-size:14px;color:#333;'><strong>{shipmentLabelLinks}:</strong><br/>{links}</div>")}
                </div>";
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
        <div style='margin-top:25px;'>
            <h3 style='margin-bottom:10px;font-size:16px;color:#222;border-bottom:1px solid #dce3eb;padding-bottom:5px;'>{shipmentHeader}</h3>
            {shipmentSection}
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
    }
}
