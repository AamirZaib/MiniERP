using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniERP.Application.DTOs;
using MiniERP.Web.Services;

namespace MiniERP.Web.Pages
{
    public class EditCustomerModel : PageModel
    {
        private readonly ApiClientService _apiClient;
        public EditCustomerModel(ApiClientService apiClient) => _apiClient = apiClient;

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public UpdateCustomerRequest Customer { get; set; } = new();

        public string? Message { get; set; }
        public bool Success { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            var customers = await _apiClient.GetCustomersAsync(token);
            var customer = customers.FirstOrDefault(c => c.Id == id);
            if (customer == null) return NotFound();

            Id = id;
            Customer = new UpdateCustomerRequest { Name = customer.Name, Email = customer.Email, Phone = customer.Phone };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            Success = await _apiClient.UpdateCustomerAsync(token, Id, Customer);
            Message = Success ? "Customer updated successfully." : "Failed to update customer.";

            if (Success) return RedirectToPage("/Customers");
            return Page();
        }
    }
}
