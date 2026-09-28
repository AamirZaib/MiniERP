using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniERP.Application.DTOs;
using MiniERP.Web.Services;

namespace MiniERP.Web.Pages
{
    public class ProductsModel : PageModel
    {
        private readonly ApiClientService _apiClient;

        public ProductsModel(ApiClientService apiClient)
        {
            _apiClient = apiClient;
        }

        public List<ProductResponse> Products { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            Products = await _apiClient.GetProductsAsync(token);
            return Page();
        }
    }
}
