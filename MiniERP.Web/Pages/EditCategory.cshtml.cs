using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniERP.Application.DTOs;
using MiniERP.Web.Services;

namespace MiniERP.Web.Pages
{
    public class EditCategoryModel : PageModel
    {
        private readonly ApiClientService _apiClient;
        public EditCategoryModel(ApiClientService apiClient) => _apiClient = apiClient;

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public UpdateCategoryRequest Category { get; set; } = new();

        public string? Message { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            var categories = await _apiClient.GetCategoriesAsync(token);
            var category = categories.FirstOrDefault(c => c.Id == id);
            if (category == null) return NotFound();

            Id = id;
            Category = new UpdateCategoryRequest { Name = category.Name };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            var success = await _apiClient.UpdateCategoryAsync(token, Id, Category);
            if (success) return RedirectToPage("/Categories");

            Message = "Failed to update category.";
            return Page();
        }
    }
}
