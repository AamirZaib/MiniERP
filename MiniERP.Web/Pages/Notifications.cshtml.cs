using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniERP.Application.DTOs;
using MiniERP.Web.Services;

namespace MiniERP.Web.Pages
{
    public class NotificationsModel : PageModel
    {
        private readonly ApiClientService _apiClient;
        public NotificationsModel(ApiClientService apiClient) => _apiClient = apiClient;

        public List<NotificationResponse> Notifications { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            Notifications = await _apiClient.GetNotificationsAsync(token);
            return Page();
        }

        public async Task<IActionResult> OnPostMarkAllReadAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            await _apiClient.MarkAllNotificationsReadAsync(token);
            return RedirectToPage();
        }
    }
}
