using MiniERP.Application.DTOs;
using System.Net.Http.Headers;

namespace MiniERP.Web.Services
{
    public class ApiClientService
    {
        private readonly HttpClient _httpClient;

        public ApiClientService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private void SetAuthHeader(string? token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(token)
                ? null
                : new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<AuthResponse>()
                : null;
        }

        public async Task<List<ProductResponse>> GetProductsAsync(string token)
        {
            SetAuthHeader(token);
            var result = await _httpClient.GetFromJsonAsync<List<ProductResponse>>("api/products");
            return result ?? new();
        }

        public async Task<(bool success, string? message, OrderResponse? order)> CreateOrderAsync(string token, CreateOrderRequest request)
        {
            SetAuthHeader(token);
            var response = await _httpClient.PostAsJsonAsync("api/orders", request);
            if (response.IsSuccessStatusCode)
            {
                var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
                return (true, null, order);
            }
            var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            return (false, error?.GetValueOrDefault("message") ?? "Order failed.", null);
        }

        

        public async Task<List<MonthlySalesResponse>?> GetSalesByMonthAsync(string token)
        {
            SetAuthHeader(token);
            var response = await _httpClient.GetAsync("api/reports/sales-by-month");
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden) return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<MonthlySalesResponse>>() ?? new();
        }

        public async Task<List<TopProductResponse>?> GetTopProductsAsync(string token)
        {
            SetAuthHeader(token);
            var response = await _httpClient.GetAsync("api/reports/top-products");
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden) return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<TopProductResponse>>() ?? new();
        }
        public async Task<List<CustomerResponse>> GetCustomersAsync(string token)
        {
            SetAuthHeader(token);
            var result = await _httpClient.GetFromJsonAsync<List<CustomerResponse>>("api/customers");
            return result ?? new();
        }
        public async Task<List<OrderResponse>> GetOrdersAsync(string token)
        {
            SetAuthHeader(token);
            var result = await _httpClient.GetFromJsonAsync<List<OrderResponse>>("api/orders");
            return result ?? new();
        }
        public async Task<CustomerResponse?> CreateCustomerAsync(string token, CreateCustomerRequest request)
        {
            SetAuthHeader(token);
            var response = await _httpClient.PostAsJsonAsync("api/customers", request);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<CustomerResponse>()
                : null;
        }

        public async Task<List<CategoryResponse>> GetCategoriesAsync(string token)
        {
            SetAuthHeader(token);
            var result = await _httpClient.GetFromJsonAsync<List<CategoryResponse>>("api/categories");
            return result ?? new();
        }

        public async Task<CategoryResponse?> CreateCategoryAsync(string token, CreateCategoryRequest request)
        {
            SetAuthHeader(token);
            var response = await _httpClient.PostAsJsonAsync("api/categories", request);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<CategoryResponse>()
                : null;
        }

        public async Task<RegisterResult> RegisterAsync(RegisterRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
            if (response.IsSuccessStatusCode) return new RegisterResult(true, null);
            var error = await response.Content.ReadFromJsonAsync<List<string>>();
            return new RegisterResult(false, error != null ? string.Join(", ", error) : "Registration failed.");
        }
        public async Task<RegisterResult> CreateUserAsync(string token, RegisterRequest request)
        {
            SetAuthHeader(token);
            var response = await _httpClient.PostAsJsonAsync("api/auth/create-user", request);
            if (response.IsSuccessStatusCode) return new RegisterResult(true, null);
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                return new RegisterResult(false, "Only Admin can create users.");

            var error = await response.Content.ReadFromJsonAsync<List<string>>();
            return new RegisterResult(false, error != null ? string.Join(", ", error) : "Failed to create user.");
        }
        public async Task<SmartSearchResponse> SmartSearchAsync(string token, string query)
        {
            SetAuthHeader(token);
            var response = await _httpClient.GetAsync($"api/products/smart-search?query={Uri.EscapeDataString(query)}");

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                var errorBody = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                return new SmartSearchResponse
                {
                    Products = new(),
                    Explanation = errorBody?.GetValueOrDefault("message") ?? "Daily search limit reached. Please try again tomorrow."
                };
            }

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<SmartSearchResponse>();
            return result ?? new();
        }
        public async Task<GlobalSearchResponse> GlobalSearchAsync(string token, string query)
        {
            SetAuthHeader(token);
            var response = await _httpClient.GetAsync($"api/search/global?query={Uri.EscapeDataString(query)}");

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                var errorBody = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                return new GlobalSearchResponse
                {
                    EntityType = "none",
                    Explanation = errorBody?.GetValueOrDefault("message") ?? "Daily search limit reached."
                };
            }

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<GlobalSearchResponse>();
            return result ?? new();
        }
        public async Task<List<SupplierResponse>> GetSuppliersAsync(string token)
        {
            SetAuthHeader(token);
            var result = await _httpClient.GetFromJsonAsync<List<SupplierResponse>>("api/suppliers");
            return result ?? new();
        }

        public async Task<SupplierResponse?> CreateSupplierAsync(string token, CreateSupplierRequest request)
        {
            SetAuthHeader(token);
            var response = await _httpClient.PostAsJsonAsync("api/suppliers", request);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<SupplierResponse>()
                : null;
        }

        public async Task<List<PurchaseOrderResponse>> GetPurchaseOrdersAsync(string token)
        {
            SetAuthHeader(token);
            var result = await _httpClient.GetFromJsonAsync<List<PurchaseOrderResponse>>("api/purchaseorders");
            return result ?? new();
        }

        public async Task<(bool success, string? message)> CreatePurchaseOrderAsync(string token, CreatePurchaseOrderRequest request)
        {
            SetAuthHeader(token);
            var response = await _httpClient.PostAsJsonAsync("api/purchaseorders", request);
            if (response.IsSuccessStatusCode) return (true, null);
            var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            return (false, error?.GetValueOrDefault("message") ?? "Failed to create purchase order.");
        }

        public async Task<bool> ReceivePurchaseOrderAsync(string token, int id)
        {
            SetAuthHeader(token);
            var response = await _httpClient.PatchAsync($"api/purchaseorders/{id}/receive", null);
            return response.IsSuccessStatusCode;
        }
        public async Task<List<InvoiceResponse>> GetInvoicesAsync(string token)
        {
            SetAuthHeader(token);
            var result = await _httpClient.GetFromJsonAsync<List<InvoiceResponse>>("api/invoices");
            return result ?? new();
        }

        public async Task<(bool success, string? message)> GenerateInvoiceAsync(string token, int orderId)
        {
            SetAuthHeader(token);
            var response = await _httpClient.PostAsync($"api/invoices/generate/{orderId}", null);
            if (response.IsSuccessStatusCode) return (true, null);

            var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            return (false, error?.GetValueOrDefault("message") ?? "Failed to generate invoice.");
        }

        public async Task<byte[]?> DownloadInvoicePdfAsync(string token, int invoiceId)
        {
            SetAuthHeader(token);
            var response = await _httpClient.GetAsync($"api/invoices/{invoiceId}/pdf");
            return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
        }
        public async Task<List<AuditLogResponse>> GetAuditLogsAsync(string token)
        {
            SetAuthHeader(token);
            var result = await _httpClient.GetFromJsonAsync<List<AuditLogResponse>>("api/auditlogs?count=100");
            return result ?? new();
        }
        public async Task<int> GetUnreadNotificationCountAsync(string token)
        {
            SetAuthHeader(token);
            var result = await _httpClient.GetFromJsonAsync<Dictionary<string, int>>("api/notifications/unread-count");
            return result?.GetValueOrDefault("count") ?? 0;
        }

        public async Task<List<NotificationResponse>> GetNotificationsAsync(string token)
        {
            SetAuthHeader(token);
            var result = await _httpClient.GetFromJsonAsync<List<NotificationResponse>>("api/notifications");
            return result ?? new();
        }

        public async Task MarkAllNotificationsReadAsync(string token)
        {
            SetAuthHeader(token);
            await _httpClient.PatchAsync("api/notifications/read-all", null);
        }
        public async Task<bool> UpdateProductAsync(string token, int id, UpdateProductRequest request)
        {
            SetAuthHeader(token);
            var response = await _httpClient.PutAsJsonAsync($"api/products/{id}", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateCustomerAsync(string token, int id, UpdateCustomerRequest request)
        {
            SetAuthHeader(token);
            var response = await _httpClient.PutAsJsonAsync($"api/customers/{id}", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateSupplierAsync(string token, int id, UpdateSupplierRequest request)
        {
            SetAuthHeader(token);
            var response = await _httpClient.PutAsJsonAsync($"api/suppliers/{id}", request);
            return response.IsSuccessStatusCode;
        }
        public async Task<ProductResponse?> CreateProductAsync(string token, CreateProductRequest request)
        {
            SetAuthHeader(token);
            var response = await _httpClient.PostAsJsonAsync("api/products", request);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<ProductResponse>() : null;
        }
        public async Task<bool> UpdateCategoryAsync(string token, int id, UpdateCategoryRequest request)
        {
            SetAuthHeader(token);
            var response = await _httpClient.PutAsJsonAsync($"api/categories/{id}", request);
            return response.IsSuccessStatusCode;
        }
        public async Task<(bool success, string? message)> UpdateOrderStatusAsync(string token, int orderId, string newStatus)
        {
            SetAuthHeader(token);
            var response = await _httpClient.PatchAsJsonAsync($"api/orders/{orderId}/status", newStatus);
            if (response.IsSuccessStatusCode) return (true, null);
            var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            return (false, error?.GetValueOrDefault("message") ?? "Failed to update order status.");
        }
        public async Task<AiCommandProposal> ParseAiCommandAsync(string token, string command)
        {
            SetAuthHeader(token);
            var response = await _httpClient.PostAsJsonAsync("api/aicommand/parse", new AiCommandRequest { Command = command });

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                var errorBody = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                return new AiCommandProposal { Success = false, ErrorMessage = errorBody?.GetValueOrDefault("message") ?? "Rate limit reached." };
            }

            var result = await response.Content.ReadFromJsonAsync<AiCommandProposal>();
            return result ?? new AiCommandProposal { Success = false, ErrorMessage = "Something went wrong." };
        }

        public async Task<(bool success, string message)> ExecuteAiCommandAsync(string token, AiCommandProposal proposal)
        {
            SetAuthHeader(token);
            var response = await _httpClient.PostAsJsonAsync("api/aicommand/execute", proposal);
            var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            return (response.IsSuccessStatusCode, body?.GetValueOrDefault("message") ?? "Failed.");
        }
        public record RegisterResult(bool Success, string? ErrorMessage);
    }
}
