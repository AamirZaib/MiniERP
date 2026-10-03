using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniERP.Application.DTOs;
using MiniERP.Web.Services;

namespace MiniERP.Web.Pages
{
    public class SuppliersModel : PageModel
    {
        private readonly ApiClientService _apiClient;
        public SuppliersModel(ApiClientService apiClient) => _apiClient = apiClient;

        public List<SupplierResponse> Suppliers { get; set; } = new();

        [BindProperty]
        public CreateSupplierRequest NewSupplier { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            Suppliers = await _apiClient.GetSuppliersAsync(token);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            var result = await _apiClient.CreateSupplierAsync(token, NewSupplier);
            if (result == null) ErrorMessage = "Failed to create supplier.";

            Suppliers = await _apiClient.GetSuppliersAsync(token);
            return Page();
        }
    }
}
