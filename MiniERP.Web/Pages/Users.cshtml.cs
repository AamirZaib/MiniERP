using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniERP.Application.DTOs;
using MiniERP.Web.Services;

namespace MiniERP.Web.Pages
{
    public class UsersModel : PageModel
    {
        private readonly ApiClientService _apiClient;
        public UsersModel(ApiClientService apiClient) => _apiClient = apiClient;

        [BindProperty]
        public RegisterRequest NewUser { get; set; } = new();

        public string? Message { get; set; }
        public bool Success { get; set; }

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("Token") == null) return RedirectToPage("/Index");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            var result = await _apiClient.CreateUserAsync(token, NewUser);
            Success = result.Success;
            Message = result.Success ? "User created successfully." : result.ErrorMessage;
            return Page();
        }
    }
}
