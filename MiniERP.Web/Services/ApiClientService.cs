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
        public record RegisterResult(bool Success, string? ErrorMessage);
    }
}
