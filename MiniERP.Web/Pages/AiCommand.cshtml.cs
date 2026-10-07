using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniERP.Application.DTOs;
using MiniERP.Web.Services;
using System.Text.Json;

namespace MiniERP.Web.Pages
{
    public class AiCommandModel : PageModel
    {
        private readonly ApiClientService _apiClient;
        public AiCommandModel(ApiClientService apiClient) => _apiClient = apiClient;

        [BindProperty]
        public string Command { get; set; } = string.Empty;

        [BindProperty]
        public string? ProposalJson { get; set; }

        public AiCommandProposal? Proposal { get; set; }
        public string? ResultMessage { get; set; }

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("Token") == null) return RedirectToPage("/Index");
            return Page();
        }

        public async Task<IActionResult> OnPostParseAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            Proposal = await _apiClient.ParseAiCommandAsync(token, Command);
            return Page();
        }

        public async Task<IActionResult> OnPostConfirmAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            var proposal = JsonSerializer.Deserialize<AiCommandProposal>(ProposalJson!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var (success, message) = await _apiClient.ExecuteAiCommandAsync(token, proposal!);
            ResultMessage = message;
            return Page();
        }
    }
}
