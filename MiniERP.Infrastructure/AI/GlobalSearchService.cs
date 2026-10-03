using Microsoft.Extensions.Configuration;
using MiniERP.Application.DTOs;
using MiniERP.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace MiniERP.Infrastructure.AI
{
    public class GlobalSearchService : IGlobalSearchService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GlobalSearchService(IUnitOfWork unitOfWork, HttpClient httpClient, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _httpClient = httpClient;
            _apiKey = configuration["ClaudeApiKey"]!;
        }

        public async Task<GlobalSearchResponse> SearchAsync(string query)
        {
            // Sab entities ek sath fetch karte hain
            var products = (await _unitOfWork.Products.GetAllWithIncludesAsync(p => p.Category)).ToList();
            var customers = (await _unitOfWork.Customers.GetAllAsync()).ToList();
            var orders = (await _unitOfWork.Orders.GetAllWithIncludesAsync(o => o.Customer, o => o.Items)).ToList();
            var categories = (await _unitOfWork.Categories.GetAllAsync()).ToList();

            // Har entity ka compact summary banate hain AI ke liye
            var sb = new StringBuilder();

            sb.AppendLine("PRODUCTS:");
            foreach (var p in products)
                sb.AppendLine($"ID:{p.Id} | {p.Name} | Category:{p.Category?.Name} | Price:Rs.{p.Price} | Stock:{p.StockQuantity}");

            sb.AppendLine("\nCUSTOMERS:");
            foreach (var c in customers)
                sb.AppendLine($"ID:{c.Id} | {c.Name} | {c.Email} | Phone:{c.Phone ?? "N/A"}");

            sb.AppendLine("\nORDERS:");
            foreach (var o in orders)
                sb.AppendLine($"ID:{o.Id} | Customer:{o.Customer?.Name} | Status:{o.Status} | Total:Rs.{o.TotalAmount} | Date:{o.OrderDate:yyyy-MM-dd}");

            sb.AppendLine("\nCATEGORIES:");
            foreach (var c in categories)
                sb.AppendLine($"ID:{c.Id} | {c.Name}");

            var prompt = $@"You are a search assistant for an ERP system with four entity types: products, customers, orders, categories.

Data:
{sb}

User query: ""{query}""

Determine which ONE entity type the user is most likely searching for, and which specific IDs match their query.

Return ONLY a JSON object (no other text) in this exact format:
{{""entityType"": ""products"", ""ids"": [1, 2], ""explanation"": ""short explanation""}}

entityType must be one of: products, customers, orders, categories, none
If nothing matches or the query is unclear, use ""none"" with empty ids.";

            var requestBody = new
            {
                model = "claude-sonnet-5",
                max_tokens = 400,
                messages = new[] { new { role = "user", content = prompt } }
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Add("x-api-key", _apiKey);
            httpRequest.Headers.Add("anthropic-version", "2023-06-01");

            var response = await _httpClient.SendAsync(httpRequest);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            var contentArray = doc.RootElement.GetProperty("content");
            var textContent = contentArray.EnumerateArray()
                .First(block => block.GetProperty("type").GetString() == "text")
                .GetProperty("text")
                .GetString()!;

            var jsonStart = textContent.IndexOf('{');
            var jsonEnd = textContent.LastIndexOf('}');
            var cleanJson = jsonStart >= 0 && jsonEnd > jsonStart
                ? textContent.Substring(jsonStart, jsonEnd - jsonStart + 1)
                : textContent;

            using var resultDoc = JsonDocument.Parse(cleanJson);

            var entityType = resultDoc.RootElement.TryGetProperty("entityType", out var etElement)
                ? etElement.GetString() ?? "none"
                : "none";

            var ids = resultDoc.RootElement.TryGetProperty("ids", out var idsElement)
                ? idsElement.EnumerateArray().Select(x => x.GetInt32()).ToHashSet()
                : new HashSet<int>();

            var explanation = resultDoc.RootElement.TryGetProperty("explanation", out var explElement)
                ? explElement.GetString() ?? ""
                : "";

            var result = new GlobalSearchResponse { EntityType = entityType, Explanation = explanation };

            // Match hui entity ke hisaab se sahi list populate karte hain
            switch (entityType)
            {
                case "products":
                    result.Products = products.Where(p => ids.Contains(p.Id)).Select(p => new ProductResponse
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        StockQuantity = p.StockQuantity,
                        LowStockThreshold = p.LowStockThreshold,
                        IsLowStock = p.IsLowStock,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category?.Name ?? ""
                    }).ToList();
                    break;

                case "customers":
                    result.Customers = customers.Where(c => ids.Contains(c.Id)).Select(c => new CustomerResponse
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Email = c.Email,
                        Phone = c.Phone
                    }).ToList();
                    break;

                case "orders":
                    result.Orders = orders.Where(o => ids.Contains(o.Id)).Select(o => new OrderResponse
                    {
                        Id = o.Id,
                        OrderDate = o.OrderDate,
                        Status = o.Status.ToString(),
                        CustomerName = o.Customer?.Name ?? "Unknown",
                        TotalAmount = o.TotalAmount,
                        Items = o.Items.Select(i => new OrderItemResponse
                        {
                            ProductName = products.FirstOrDefault(p => p.Id == i.ProductId)?.Name ?? "Unknown",
                            Quantity = i.Quantity,
                            UnitPrice = i.UnitPrice
                        }).ToList()
                    }).ToList();
                    break;

                case "categories":
                    result.Categories = categories.Where(c => ids.Contains(c.Id)).Select(c => new CategoryResponse
                    {
                        Id = c.Id,
                        Name = c.Name
                    }).ToList();
                    break;
            }

            return result;
        }
    }
}
