using Microsoft.Extensions.Configuration;
using MiniERP.Application.DTOs;
using MiniERP.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace MiniERP.Infrastructure.AI
{
    public class ClaudeSearchService : IAiSearchService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public ClaudeSearchService(IUnitOfWork unitOfWork, HttpClient httpClient, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _httpClient = httpClient;
            _apiKey = configuration["ClaudeApiKey"]!;
        }

        public async Task<SmartSearchResponse> SearchAsync(string query)
        {
            var products = await _unitOfWork.Products.GetAllWithIncludesAsync(p => p.Category);

            var catalogText = string.Join("\n", products.Select(p =>
                $"ID:{p.Id} | {p.Name} | Category:{p.Category?.Name} | Price:Rs.{p.Price} | Stock:{p.StockQuantity}"));

            var prompt = $@"You are a product search assistant for an ERP system. 
Given this product catalog:
{catalogText}

User query: ""{query}""

Return ONLY a JSON object (no other text) in this exact format:
{{""productIds"": [1, 2], ""explanation"": ""short explanation of why these matched""}}

If nothing matches, return empty productIds array.";

            var requestBody = new
            {
                model = "claude-sonnet-5",
                max_tokens = 300,
                messages = new[] { new { role = "user", content = prompt } }
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Add("x-api-key", _apiKey);
            httpRequest.Headers.Add("anthropic-version", "2023-06-01");

            var response = await _httpClient.SendAsync(httpRequest);
            //response.EnsureSuccessStatusCode();
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Claude API error ({response.StatusCode}): {errorBody}");
            }
            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var contentArray = doc.RootElement.GetProperty("content");
            var textContent = contentArray.EnumerateArray()
                .First(block => block.GetProperty("type").GetString() == "text")
                .GetProperty("text")
                .GetString()!;
            //using var resultDoc = JsonDocument.Parse(textContent);
            //var productIds = resultDoc.RootElement.GetProperty("productIds")
            //    .EnumerateArray().Select(x => x.GetInt32()).ToHashSet();
            //var explanation = resultDoc.RootElement.GetProperty("explanation").GetString() ?? "";

            // Claude kabhi kabhi JSON ke around markdown fences (```json ... ```) ya extra text add kar deta hai
            // Isliye pehle sirf { se } tak ka hissa nikalte hain
            var jsonStart = textContent.IndexOf('{');
            var jsonEnd = textContent.LastIndexOf('}');
            var cleanJson = jsonStart >= 0 && jsonEnd > jsonStart
                ? textContent.Substring(jsonStart, jsonEnd - jsonStart + 1)
                : textContent;

            using var resultDoc = JsonDocument.Parse(cleanJson);

            var productIds = resultDoc.RootElement.TryGetProperty("productIds", out var idsElement)
                ? idsElement.EnumerateArray().Select(x => x.GetInt32()).ToHashSet()
                : new HashSet<int>();

            var explanation = resultDoc.RootElement.TryGetProperty("explanation", out var explElement)
                ? explElement.GetString() ?? ""
                : "";


            var matchedProducts = products.Where(p => productIds.Contains(p.Id)).Select(p => new ProductResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                LowStockThreshold = p.LowStockThreshold,
                IsLowStock = p.IsLowStock,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? string.Empty
            }).ToList();

            return new SmartSearchResponse { Products = matchedProducts, Explanation = explanation };
        }
    }
}
