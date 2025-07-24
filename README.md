# EInvoicesService

> 💼 **Commercial Project** — part of a private or client-facing initiative.

**EInvoicesService** is a Windows Service built on the .NET Framework for automating the generation and delivery of invoices and export declarations for clients. It integrates with Comarch ERP via API to generate PDF documents using Crystal Reports and handles email distribution, archiving, and conditional export documentation.

## Features

- 🧾 **Automated Invoice Generation**:

  - Fetches data for orders up to the current date that have not yet been invoiced.
  - Generates PDF invoices via **Crystal Reports** through the **Comarch ERP API**.

- 📧 **Email Delivery**:

  - Automatically sends generated invoices to the respective client emails.

- 📄 **Export Declaration (Optional)**:

  - Creates export declarations when required for the client.
  - Saves the declaration in a folder and sends it via email as a PDF attachment.

- 🗂️ **Archiving & Backup**:
  - After all clients are processed, invoices and export declarations are moved to a designated backup folder.

## Technologies Used

- **.NET Framework** – Windows Service implementation
- **Crystal Reports** – PDF report generation
- **Comarch ERP API** – Data fetching and document generation
- **SMTP** – Email distribution
- **SQL Server** – Stored procedure for invoice data

## License

This project is proprietary and confidential. See the [LICENSE](LICENSE) file for more information.

---

© 2025 [calKU0](https://github.com/calKU0)
