using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniERP.Application.DTOs;
using MiniERP.Web.Services;

namespace MiniERP.Web.Pages
{
    public class ProductsModel : PageModel
    {

        [BindProperty]
        public string? SearchQuery { get; set; }
        public string? AiExplanation { get; set; }

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

        public async Task<IActionResult> OnPostSearchAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var result = await _apiClient.SmartSearchAsync(token, SearchQuery);
                Products = result.Products;
                AiExplanation = result.Explanation;
            }
            else
            {
                Products = await _apiClient.GetProductsAsync(token);
            }

            return Page();
        }
    }
}
