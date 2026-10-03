using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniERP.Application.DTOs;
using MiniERP.Web.Services;

namespace MiniERP.Web.Pages
{
    public class InvoicesModel : PageModel
    {
        private readonly ApiClientService _apiClient;
        public InvoicesModel(ApiClientService apiClient) => _apiClient = apiClient;

        public List<InvoiceResponse> Invoices { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            Invoices = (await _apiClient.GetInvoicesAsync(token)).ToList();
            return Page();
        }

        // Yeh handler PDF ko proxy karta hai — browser ko seedha API se Authorization header bhejna mumkin nahi
        public async Task<IActionResult> OnGetDownloadAsync(int id)
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            var pdfBytes = await _apiClient.DownloadInvoicePdfAsync(token, id);
            if (pdfBytes == null) return NotFound();

            return File(pdfBytes, "application/pdf", $"invoice-{id}.pdf");
        }
    }
}
