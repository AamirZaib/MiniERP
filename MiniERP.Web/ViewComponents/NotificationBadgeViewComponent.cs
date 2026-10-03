using Microsoft.AspNetCore.Mvc;
using MiniERP.Web.Services;

namespace MiniERP.Web.ViewComponents
{
    public class NotificationBadgeViewComponent : ViewComponent
    {
        private readonly ApiClientService _apiClient;
        public NotificationBadgeViewComponent(ApiClientService apiClient) => _apiClient = apiClient;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return Content("");

            var count = await _apiClient.GetUnreadNotificationCountAsync(token);
            return View("Default", count);
        }
    }
}
