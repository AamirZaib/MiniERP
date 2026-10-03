using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using MiniERP.Application.DTOs;
using MiniERP.Web.Services;

namespace MiniERP.Web.Pages
{
    public class EditProductModel : PageModel
    {
        private readonly ApiClientService _apiClient;
        public EditProductModel(ApiClientService apiClient) => _apiClient = apiClient;

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public UpdateProductRequest Product { get; set; } = new();

        public List<SelectListItem> CategoryOptions { get; set; } = new();
        public string? Message { get; set; }
        public bool Success { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            var products = await _apiClient.GetProductsAsync(token);
            var product = products.FirstOrDefault(p => p.Id == id);
            if (product == null) return NotFound();

            Id = id;
            Product = new UpdateProductRequest
            {
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                LowStockThreshold = product.LowStockThreshold,
                CategoryId = product.CategoryId
            };

            var categories = await _apiClient.GetCategoriesAsync(token);
            CategoryOptions = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            Success = await _apiClient.UpdateProductAsync(token, Id, Product);
            Message = Success ? "Product updated successfully." : "Failed to update product.";

            if (Success) return RedirectToPage("/Products");

            var categories = await _apiClient.GetCategoriesAsync(token);
            CategoryOptions = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
            return Page();
        }
    }
}
