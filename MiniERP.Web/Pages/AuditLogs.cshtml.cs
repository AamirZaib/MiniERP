using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniERP.Application.DTOs;
using MiniERP.Web.Services;

namespace MiniERP.Web.Pages
{
    public class AuditLogsModel : PageModel
    {
        private readonly ApiClientService _apiClient;
        public AuditLogsModel(ApiClientService apiClient) => _apiClient = apiClient;

        public List<AuditLogResponse>? Logs { get; set; }
        public bool AccessDenied { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            var result = await _apiClient.GetAuditLogsAsync(token);
            if (result == null || !result.Any() && HttpContext.Session.GetString("Roles")?.Contains("Admin") != true)
            {
                AccessDenied = true;
            }
            else
            {
                Logs = result;
            }
            return Page();
        }
    }
}
