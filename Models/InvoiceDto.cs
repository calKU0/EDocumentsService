using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFakturyService.Models
{
    public class InvoiceDto
    {
        public short GidType { get; set; }
        public int GidNumber { get; set; }
        public string DocumentName { get; set; }
        public string FileName { get; set; }
        public DateTime Date { get; set; }
        public string ClientName { get; set; }
        public int ClientId { get; set; }
        public string Country { get; set; }
        public string Email { get; set; }
        public string RepresentativeEmail { get; set; }
        public bool ExportDeclaration { get; set; }
        public string TrackingNumbers { get; set; }
        public string TrackingLinks { get; set; }
        public int NumberOfStapledWZDocuments { get; set; }
    }
}