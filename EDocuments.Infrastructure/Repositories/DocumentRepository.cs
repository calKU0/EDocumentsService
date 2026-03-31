using Dapper;
using EDocuments.Contracts.Models;
using EDocuments.Contracts.Repositories;
using EDocuments.Infrastructure.Data;
using System.Data;

namespace EDocuments.Infrastructure.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly DapperContext _context;
        public DocumentRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<List<ExportDeclaration>> GetExportDeclarations()
        {
            const string procedure = "dbo.GaskaGetEExportDeclarations";
            using (var connection = _context.CreateConnection())
            {
                connection.Open();
                var declarations = await connection.QueryAsync<ExportDeclaration>(procedure, commandType: CommandType.StoredProcedure);
                return declarations.ToList();
            }
        }

        public async Task<List<Invoice>> GetInvoices()
        {
            const string procedure = "dbo.GaskaGetEInvoices";
            using (var connection = _context.CreateConnection())
            {
                connection.Open();
                var invoices = await connection.QueryAsync<Invoice>(procedure, commandType: CommandType.StoredProcedure);
                return invoices.ToList();
            }
        }

        public async Task<List<WZDocument>> GetWZDocuments()
        {
            const string procedure = "dbo.GaskaGetEWZ";
            using (var connection = _context.CreateConnection())
            {
                connection.Open();
                var wzList = await connection.QueryAsync<WZDocument>(procedure, commandType: CommandType.StoredProcedure);
                return wzList.ToList();
            }
        }
    }
}
