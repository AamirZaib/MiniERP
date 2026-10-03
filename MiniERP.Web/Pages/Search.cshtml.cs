using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniERP.Application.DTOs;
using MiniERP.Web.Services;

namespace MiniERP.Web.Pages
{
    public class SearchModel : PageModel
    {
        private readonly ApiClientService _apiClient;
        public SearchModel(ApiClientService apiClient) => _apiClient = apiClient;

        public GlobalSearchResponse? Result { get; set; }
        public string? Query { get; set; }

        public async Task<IActionResult> OnGetAsync(string? q)
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            if (!string.IsNullOrWhiteSpace(q))
            {
                Query = q;
                Result = await _apiClient.GlobalSearchAsync(token, q);
            }

            return Page();
        }
    }
}
