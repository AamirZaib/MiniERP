using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniERP.Application.DTOs;
using MiniERP.Web.Services;

namespace MiniERP.Web.Pages
{
    public class EditSupplierModel : PageModel
    {
        private readonly ApiClientService _apiClient;
        public EditSupplierModel(ApiClientService apiClient) => _apiClient = apiClient;

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public UpdateSupplierRequest Supplier { get; set; } = new();

        public string? Message { get; set; }
        public bool Success { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            var suppliers = await _apiClient.GetSuppliersAsync(token);
            var supplier = suppliers.FirstOrDefault(s => s.Id == id);
            if (supplier == null) return NotFound();

            Id = id;
            Supplier = new UpdateSupplierRequest
            {
                Name = supplier.Name,
                ContactPerson = supplier.ContactPerson,
                Email = supplier.Email,
                Phone = supplier.Phone
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            Success = await _apiClient.UpdateSupplierAsync(token, Id, Supplier);
            Message = Success ? "Supplier updated successfully." : "Failed to update supplier.";

            if (Success) return RedirectToPage("/Suppliers");
            return Page();
        }
    }
}
