using EFakturyService.Interfaces;
using EFakturyService.Models;
using EFakturyService.Models.Settings;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

namespace EFakturyService.Services
{
    public class EmailSenderService : IEmailSenderService
    {
        private readonly SmtpSettings _smtpSettings;

        public EmailSenderService(SmtpSettings smtpSettings)
        {
            _smtpSettings = smtpSettings;
        }

        public void SendInvoiceEmail(InvoiceDto invoice, string attachmentPath)
        {
            string subject = invoice.Country == "PL"
                ? $"E-faktura {invoice.DocumentName}"
                : $"E-invoice {invoice.DocumentName}";

            var body = BuildInvoiceBody(invoice);

            SendEmailWithAttachment(invoice.Email, invoice.RepresentativeEmail, subject, body, attachmentPath, invoice.DocumentName);
        }

        public void SendExportDeclarationEmail(InvoiceDto invoice, string attachmentPath)
        {
            string subject = invoice.Country == "PL"
                ? "Potwierdzenie dostawy towaru z terytorium Polski"
                : "Confirmation of delivery the goods from the territory of Poland";

            var body = BuildExportDeclarationBody(invoice.Country);

            SendEmailWithAttachment(invoice.Email, invoice.RepresentativeEmail, subject, body, attachmentPath, Path.GetFileName(attachmentPath));
        }

        private void SendEmailWithAttachment(string recipientsRaw, string repEmail, string subject, string body, string attachmentPath, string documentName)
        {
            var mail = new MailMessage
            {
                From = new MailAddress(_smtpSettings.Login),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            // Add To recipients, skipping invalid emails but notify rep
            var recipients = recipientsRaw?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(e => e.Trim())
                                .Where(e => !string.IsNullOrWhiteSpace(e))
                                .ToArray() ?? Array.Empty<string>();

            foreach (var email in recipients)
            {
                try
                {
                    mail.To.Add(new MailAddress(email));
                }
                catch
                {
                    NotifyRepresentative(repEmail, documentName, recipientsRaw);
                    Log.Warning($"Nieprawidłowy adres email: {email} (dokument: {documentName})");
                }
            }

            // Add attachment
            var attachment = new Attachment(attachmentPath);
            mail.Attachments.Add(attachment);

            var smtpClient = new SmtpClient(_smtpSettings.Name)
            {
                Credentials = new NetworkCredential(_smtpSettings.Login, _smtpSettings.Password),
                Port = _smtpSettings.Port
            };

            smtpClient.Send(mail);
            attachment.Dispose();

            Log.Information($"Wysłano e-fakturę {documentName} do {recipientsRaw}");
        }

        private string BuildInvoiceBody(InvoiceDto invoice)
        {
            var shipmentNumbersHtml = BuildShipmentNumbersHtml(invoice.TrackingNumbers);
            var shipmentLinksHtml = BuildShipmentLinksHtml(invoice.TrackingLinks);

            bool hasShipments = !string.IsNullOrEmpty(shipmentNumbersHtml) && !string.IsNullOrEmpty(shipmentLinksHtml);

            var greeting = invoice.Country == "PL" ? "Szanowny kliencie," : "Dear customer,";

            var invoiceInfo = invoice.Country == "PL"
                ? $"<br/><br/>Przesyłamy w załączniku elektroniczną fakturę numer: {invoice.DocumentName}"
                : $"<br/><br/>We send in attachment electronic invoice number: {invoice.DocumentName}";

            var shipmentInfo = hasShipments
                ? (invoice.Country == "PL"
                    ? $"<br/><br/>Towar zostanie dostarczony za pomocą spedycji: {shipmentNumbersHtml}" +
                      $"<br/><br/>Możecie Państwo śledzić swoją przesyłkę klikając na link poniżej: {shipmentLinksHtml}"
                    : $"<br/><br/>Goods will be delivered by: {shipmentNumbersHtml}" +
                      $"<br/><br/>You can track your order by clicking on the link below: {shipmentLinksHtml}")
                : "";

            var contact = invoice.Country == "PL"
                ? "<br/><br/>W przypadku pytań związanych z e-fakturą prosimy o kontakt na adres e-faktura@gaska.com.pl"
                : "<br/><br/>For questions related to e-invoice, please contact us at the address e-faktura@gaska.com.pl";

            var signature = "<br/><br/>Pozdrawiamy/Best Regards" +
                            "<br/>Gąska sp. z o.o." +
                            "<br/>Gotkowice 85" +
                            "<br/>32-048 Jerzmanowice" +
                            "<br/>POLAND" +
                            "<br/>NIP: 677-000-03-35";

            return greeting + invoiceInfo + shipmentInfo + contact + signature;
        }

        private string BuildExportDeclarationBody(string country)
        {
            if (country == "PL")
            {
                return
                    "Drodzy Państwo," +
                    "<br/><br/>W związku z zakupami dokonanymi w naszej firmie w poprzednim miesiącu przesyłamy do Państwa dokument potwierdzenia dostawy towaru z terytorium Polski." +
                    "<br/>Zgodnie z obowiązującymi przepisami w Unii Europejskiej prosimy o podpisanie załączonego dokumentu i odesłanie na adres:" +
                    "<br/>platnosci@gaska.com.pl do 7 dni od otrzymania towaru." +
                    "<br/><br/>Pozdrawiamy/Best Regards" +
                    "<br/>Gąska sp. z o.o." +
                    "<br/>Gotkowice 85" +
                    "<br/>32-048 Jerzmanowice" +
                    "<br/>POLAND" +
                    "<br/>NIP: 677-000-03-35";
            }
            else
            {
                return
                    "Dear Sir or Madame," +
                    "<br/><br/>Referring to your last month purchase in our company we are sending to you confirmation delivery goods from the territory of Poland." +
                    "<br/>According to applicable laws in European Union we kindly ask you to make a signature, stamp under the document and send it back to us at our e-mail address:" +
                    "<br/>platnosci@gaska.com.pl not later than 7 days from receipt of this e-mail" +
                    "<br/><br/>Pozdrawiamy/Best Regards" +
                    "<br/>Gąska sp. z o.o." +
                    "<br/>Gotkowice 85" +
                    "<br/>32-048 Jerzmanowice" +
                    "<br/>POLAND" +
                    "<br/>NIP: 677-000-03-35";
            }
        }

        private string BuildShipmentNumbersHtml(string trackingNumbers)
        {
            if (string.IsNullOrWhiteSpace(trackingNumbers)) return string.Empty;

            var numbers = trackingNumbers.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(n => n.Trim());

            return string.Join("<br/><br/>", numbers);
        }

        private string BuildShipmentLinksHtml(string trackingLinks)
        {
            if (string.IsNullOrWhiteSpace(trackingLinks)) return string.Empty;

            var links = trackingLinks.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(link => $"<br/><br/><a href='{link.Trim()}'>{link.Trim()}</a>");

            return string.Concat(links);
        }

        private void NotifyRepresentative(string repEmail, string documentName, string invalidEmailsRaw)
        {
            if (string.IsNullOrWhiteSpace(repEmail))
                return;

            var mail = new MailMessage
            {
                From = new MailAddress(_smtpSettings.Login),
                Subject = $"Błędny adres e-mail - Faktura {documentName}",
                IsBodyHtml = true
            };

            mail.To.Add(new MailAddress(repEmail));

            var emailsList = invalidEmailsRaw?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(e => e.Trim())
                                .Where(e => !string.IsNullOrWhiteSpace(e))
                                .ToArray() ?? Array.Empty<string>();

            mail.Body = $"Dla dokumentu numer: {documentName}, nie została wysłana faktura, " +
                        $"ponieważ podano nieprawidłowy adres e-mail:<br/><br/>" +
                        $"{string.Join("<br/>", emailsList)}" +
                        $"<br/><br/>Pozdrawiamy<br/>Dział IT";

            var smtpClient = new SmtpClient(_smtpSettings.Name)
            {
                Credentials = new NetworkCredential(_smtpSettings.Login, _smtpSettings.Password),
                Port = _smtpSettings.Port
            };

            smtpClient.Send(mail);

            Log.Information($"Powiadomiono opiekuna {repEmail} o błędnych adresach e-mail.");
        }
    }
}