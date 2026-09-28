using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniERP.Application.DTOs;
using MiniERP.Web.Services;

namespace MiniERP.Web.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly ApiClientService _apiClient;
        public RegisterModel(ApiClientService apiClient) => _apiClient = apiClient;

        [BindProperty]
        public RegisterRequest RegisterInput { get; set; } = new();

        public string? Message { get; set; }
        public bool Success { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            var result = await _apiClient.RegisterAsync(RegisterInput);
            Success = result.Success;
            Message = result.Success ? "Account created! You can now log in." : result.ErrorMessage;
            return Page();
        }
    }
}
