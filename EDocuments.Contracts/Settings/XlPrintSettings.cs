namespace EDocuments.Contracts.Settings
{
    public class XlPrintSettings
    {
        public int DocumentType { get; set; }
        public bool Stapled { get; set; }
        public string Language { get; set; }
        public int PrintId { get; set; }
        public int PrintSource { get; set; }
        public int PrintFormat { get; set; }
    }
}
