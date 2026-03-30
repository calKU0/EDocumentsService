namespace EInvoice.Service.Settings
{
    public class AppSettings
    {
        public int GeneratingHour { get; set; }
        public int WorkerIntervalMinutes { get; set; }
        public int LogsExpirationDays { get; set; }
        public string BackupPath { get; set; }
        public string InvoicesPath { get; set; }
    }
}
