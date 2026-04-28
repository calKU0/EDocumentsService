namespace EDocuments.Contracts.Models
{
    public class Return
    {
        public int Id { get; set; }
        public short Type { get; set; }
        public string Name { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public DateTime Date { get; set; }
        public string ClientName { get; set; } = null!;
        public int ClientId { get; set; }
        public string Country { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? RepresentativeEmail { get; set; }
    }
}
