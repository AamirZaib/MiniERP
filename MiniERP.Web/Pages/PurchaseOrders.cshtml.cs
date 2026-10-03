using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using MiniERP.Application.DTOs;
using MiniERP.Web.Services;

namespace MiniERP.Web.Pages
{
    public class PurchaseOrdersModel : PageModel
    {
        private readonly ApiClientService _apiClient;
        public PurchaseOrdersModel(ApiClientService apiClient) => _apiClient = apiClient;

        public List<PurchaseOrderResponse> PurchaseOrders { get; set; } = new();
        public List<SelectListItem> SupplierOptions { get; set; } = new();
        public List<SelectListItem> ProductOptions { get; set; } = new();

        [BindProperty]
        public int SupplierId { get; set; }

        [BindProperty]
        public int ProductId { get; set; }

        [BindProperty]
        public int Quantity { get; set; } = 1;

        [BindProperty]
        public decimal UnitCost { get; set; }

        public string? ResultMessage { get; set; }
        public bool Success { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            await LoadDataAsync(token);
            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            var request = new CreatePurchaseOrderRequest
            {
                SupplierId = SupplierId,
                Items = new() { new CreatePurchaseOrderItemRequest { ProductId = ProductId, Quantity = Quantity, UnitCost = UnitCost } }
            };

            var (success, message) = await _apiClient.CreatePurchaseOrderAsync(token, request);
            Success = success;
            ResultMessage = success ? "Purchase order created successfully." : message;

            await LoadDataAsync(token);
            return Page();
        }

        public async Task<IActionResult> OnPostReceiveAsync(int id)
        {
            var token = HttpContext.Session.GetString("Token");
            if (token == null) return RedirectToPage("/Index");

            await _apiClient.ReceivePurchaseOrderAsync(token, id);
            return RedirectToPage();
        }

        private async Task LoadDataAsync(string token)
        {
            PurchaseOrders = await _apiClient.GetPurchaseOrdersAsync(token);

            var suppliers = await _apiClient.GetSuppliersAsync(token);
            SupplierOptions = suppliers.Select(s => new SelectListItem(s.Name, s.Id.ToString())).ToList();

            var products = await _apiClient.GetProductsAsync(token);
            ProductOptions = products.Select(p => new SelectListItem($"{p.Name} (Stock: {p.StockQuantity})", p.Id.ToString())).ToList();
        }
    }
}
