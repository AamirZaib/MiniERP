using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniERP.Application.DTOs;
using MiniERP.Web.Services;

namespace MiniERP.Web.Pages
{
    public class CategoriesModel : PageModel
    {
        private readonly ApiClientService _apiClient;
        public CategoriesModel(ApiClientService apiClient) => _apiClient = apiClient;

        public List<CategoryResponse> Categories { get; set; } = new();

        [BindProperty]
        public string NewCategoryName { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            Categories = await _apiClient.GetCategoriesAsync(token);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            var result = await _apiClient.CreateCategoryAsync(token, new CreateCategoryRequest { Name = NewCategoryName });
            if (result == null) ErrorMessage = "Failed to create category (Admin role required).";

            Categories = await _apiClient.GetCategoriesAsync(token);
            return Page();
        }
    }
}
