using EFakturyService.Data;
using EFakturyService.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFakturyService.Services
{
    public class DataService : IDataService
    {
        public async Task<List<InvoiceDto>> GetInvoices()
        {
            using (var context = new AppDbContext())
            {
                var results = await context.Database
                    .SqlQuery<InvoiceDto>("EXEC dbo.GaskaZaczytajFakturyNowe")
                    .ToListAsync();

                return results;
            }
        }

        public async Task UpdateAttributes(int gidType, int gidNumber, DateTime? date = null, string path = null)
        {
            using (var context = new AppDbContext())
            {
                var parameters = new[]
                {
                    new SqlParameter("@GidType", gidNumber),
                    new SqlParameter("@GidNumber", gidNumber),
                    new SqlParameter("@Date", date.HasValue ? (object)date.Value : DBNull.Value),
                    new SqlParameter("@Path", string.IsNullOrWhiteSpace(path) ? DBNull.Value : (object)path)
                };

                await context.Database.ExecuteSqlCommandAsync("EXEC CDN.Gaska_UpdateAtrybutyEFaktura @GidType, @GidNumber, @Date, @Path", parameters);
            }
        }
    }
}