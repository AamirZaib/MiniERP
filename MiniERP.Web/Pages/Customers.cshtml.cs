using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniERP.Application.DTOs;
using MiniERP.Web.Services;

namespace MiniERP.Web.Pages
{
    public class CustomersModel : PageModel
    {
        private readonly ApiClientService _apiClient;
        public CustomersModel(ApiClientService apiClient) => _apiClient = apiClient;

        public List<CustomerResponse> Customers { get; set; } = new();

        [BindProperty]
        public CreateCustomerRequest NewCustomer { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            Customers = await _apiClient.GetCustomersAsync(token);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            var result = await _apiClient.CreateCustomerAsync(token, NewCustomer);
            if (result == null) ErrorMessage = "Failed to create customer.";

            Customers = await _apiClient.GetCustomersAsync(token);
            return Page();
        }
    }
}
