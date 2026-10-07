using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniERP.Application.DTOs;
using MiniERP.Web.Services;

namespace MiniERP.Web.Pages
{
    public class OrdersModel : PageModel
    {
        private readonly ApiClientService _apiClient;
        public OrdersModel(ApiClientService apiClient) => _apiClient = apiClient;

        public List<OrderResponse> Orders { get; set; } = new();
        public string? Message { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            Orders = (await _apiClient.GetOrdersAsync(token)).ToList();
            return Page();
        }

        public async Task<IActionResult> OnPostGenerateInvoiceAsync(int orderId)
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            var (success, message) = await _apiClient.GenerateInvoiceAsync(token, orderId);
            Message = success ? "Invoice generated! Check the Invoices page." : message;

            Orders = (await _apiClient.GetOrdersAsync(token)).ToList();
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int orderId, string newStatus)
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            var (success, message) = await _apiClient.UpdateOrderStatusAsync(token, orderId, newStatus);
            Message = success ? $"Order #{orderId} status updated to {newStatus}." : message;

            Orders = (await _apiClient.GetOrdersAsync(token)).ToList();
            return Page();
        }
    }
}
