using EDocuments.Contracts.Models;

namespace EDocuments.Contracts.Repositories
{
    public interface IDocumentRepository
    {
        public Task<List<Invoice>> GetInvoices();
        public Task<List<ExportDeclaration>> GetExportDeclarations();
        public Task<List<WZDocument>> GetWZDocuments();
        public Task<List<Return>> GetReturns();
    }
}
