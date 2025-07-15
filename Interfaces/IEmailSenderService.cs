using EFakturyService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFakturyService.Interfaces
{
    public interface IEmailSenderService
    {
        void SendInvoiceEmail(InvoiceDto invoice, string attachmentPath);

        void SendExportDeclarationEmail(InvoiceDto invoice, string attachmentPath);
    }
}