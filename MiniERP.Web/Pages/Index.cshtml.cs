using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniERP.Application.DTOs;
using MiniERP.Web.Services;

namespace MiniERP.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApiClientService _apiClient;

        public IndexModel(ApiClientService apiClient)
        {
            _apiClient = apiClient;
        }

        [BindProperty]
        public LoginRequest LoginInput { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            var result = await _apiClient.LoginAsync(LoginInput);
            if (result == null)
            {
                ErrorMessage = "Invalid email or password.";
                return Page();
            }

            HttpContext.Session.SetString("Token", result.Token);
            HttpContext.Session.SetString("FullName", result.FullName);
            HttpContext.Session.SetString("Roles", string.Join(",", result.Roles));

            return RedirectToPage("/Products");
        }
    }
}
