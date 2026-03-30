namespace EDocuments.Contracts.Models
{
    public class ExportDeclaration
    {
        public string ClientName { get; set; }
        public int ClientId { get; set; }
        public int ClientType { get; set; }
        public string ClientCountry { get; set; }
        public string FileName { get; set; }
        public string Email { get; set; }
    }
}
