using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using MiniERP.Application.DTOs;
using MiniERP.Web.Services;

namespace MiniERP.Web.Pages
{
    public class CreateOrderModel : PageModel
    {
        private readonly ApiClientService _apiClient;

        public CreateOrderModel(ApiClientService apiClient) => _apiClient = apiClient;

        [BindProperty]
        public int CustomerId { get; set; }

        [BindProperty]
        public int ProductId { get; set; }

        [BindProperty]
        public int Quantity { get; set; } = 1;

        public List<SelectListItem> CustomerOptions { get; set; } = new();
        public List<SelectListItem> ProductOptions { get; set; } = new();

        public string? ResultMessage { get; set; }
        public bool Success { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            await LoadDropdownsAsync(token);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            var request = new CreateOrderRequest
            {
                CustomerId = CustomerId,
                Items = new() { new CreateOrderItemRequest { ProductId = ProductId, Quantity = Quantity } }
            };

            var (success, message, order) = await _apiClient.CreateOrderAsync(token, request);
            Success = success;
            ResultMessage = success ? $"Order #{order!.Id} created! Total: Rs. {order.TotalAmount}" : message;

            await LoadDropdownsAsync(token); // form dobara render hone se pehle dropdowns fresh chahiye
            return Page();
        }

        private async Task LoadDropdownsAsync(string token)
        {
            var customers = await _apiClient.GetCustomersAsync(token);
            CustomerOptions = customers.Select(c => new SelectListItem($"{c.Name} ({c.Email})", c.Id.ToString())).ToList();

            var products = await _apiClient.GetProductsAsync(token);
            ProductOptions = products.Select(p => new SelectListItem($"{p.Name} — Stock: {p.StockQuantity}", p.Id.ToString())).ToList();
        }
    }
}
