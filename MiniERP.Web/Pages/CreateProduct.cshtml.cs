using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using MiniERP.Application.DTOs;
using MiniERP.Web.Services;

namespace MiniERP.Web.Pages
{
    public class CreateProductModel : PageModel
    {
        private readonly ApiClientService _apiClient;
        public CreateProductModel(ApiClientService apiClient) => _apiClient = apiClient;

        [BindProperty]
        public CreateProductRequest Product { get; set; } = new();

        public List<SelectListItem> CategoryOptions { get; set; } = new();
        public string? Message { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            var categories = await _apiClient.GetCategoriesAsync(token);
            CategoryOptions = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            var result = await _apiClient.CreateProductAsync(token, Product);
            if (result != null) return RedirectToPage("/Products");

            Message = "Failed to create product.";
            var categories = await _apiClient.GetCategoriesAsync(token);
            CategoryOptions = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
            return Page();
        }
    }
}
