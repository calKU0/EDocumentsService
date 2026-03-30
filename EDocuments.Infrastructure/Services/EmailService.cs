using EDocuments.Contracts.Services;
using EDocuments.Contracts.Settings;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace EDocuments.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtpSettings;
        public EmailService(IOptions<SmtpSettings> smtpSettings)
        {
            _smtpSettings = smtpSettings.Value;
        }
        public void Send(string body, string subject, List<string> to, List<string>? attachments = null)
        {
            using var mail = new MailMessage
            {
                From = new MailAddress(_smtpSettings.Username),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };

            foreach (var recipient in to)
            {
                mail.To.Add(recipient);
            }

            if (attachments != null && attachments.Any())
            {
                foreach (var attachment in attachments)
                {
                    mail.Attachments.Add(new Attachment(attachment));
                }
            }

            using var smtpClient = new SmtpClient(_smtpSettings.Host)
            {
                Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password),
                Port = _smtpSettings.Port,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            smtpClient.Send(mail);
        }
    }
}
