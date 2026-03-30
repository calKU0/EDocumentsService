namespace EDocuments.Contracts.Services
{
    public interface IEmailService
    {
        public void Send(string body, string subject, List<string> to, List<string>? attachments = null);
    }
}
