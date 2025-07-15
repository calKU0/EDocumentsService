using EFakturyService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFakturyService.Services
{
    public interface IDataService
    {
        Task<List<InvoiceDto>> GetInvoices();

        Task UpdateAttributes(int gidType, int gidNumber, DateTime? date = null, string path = null);
    }
}