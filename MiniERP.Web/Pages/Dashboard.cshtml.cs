using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniERP.Application.DTOs;
using MiniERP.Web.Services;

namespace MiniERP.Web.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly ApiClientService _apiClient;

        public DashboardModel(ApiClientService apiClient)
        {
            _apiClient = apiClient;
        }

        public List<MonthlySalesResponse> SalesReport { get; set; } = new();
        public List<TopProductResponse> TopProducts { get; set; } = new();
        public bool AccessDenied { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            var sales = await _apiClient.GetSalesByMonthAsync(token);
            var top = await _apiClient.GetTopProductsAsync(token);

            if (sales == null || top == null)
            {
                AccessDenied = true;
                return Page();
            }

            SalesReport = sales;
            TopProducts = top;
            return Page();
        }
        
    }
}
