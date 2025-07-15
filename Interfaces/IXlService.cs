using EFakturyService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFakturyService.Services
{
    public interface IXlService
    {
        bool Login();

        bool Logout();

        string GenerateInvoicePdf(InvoiceDto inovice);

        string GenerateExportDeclarationPdf(InvoiceDto inovice);
    }
}