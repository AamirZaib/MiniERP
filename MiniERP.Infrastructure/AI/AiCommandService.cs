using Microsoft.Extensions.Configuration;
using MiniERP.Application.DTOs;
using MiniERP.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace MiniERP.Infrastructure.AI
{
    public class AiCommandService : IAiCommandService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public AiCommandService(IUnitOfWork unitOfWork, HttpClient httpClient, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _httpClient = httpClient;
            _apiKey = configuration["ClaudeApiKey"]!;
        }

        public async Task<AiCommandProposal> ParseAsync(string command)
        {
            var products = (await _unitOfWork.Products.GetAllAsync()).ToList();
            var suppliers = (await _unitOfWork.Suppliers.GetAllAsync()).ToList();
            var customers = (await _unitOfWork.Customers.GetAllAsync()).ToList();

            var catalogText = new StringBuilder();
            catalogText.AppendLine("PRODUCTS: " + string.Join(", ", products.Select(p => p.Name)));
            catalogText.AppendLine("SUPPLIERS: " + string.Join(", ", suppliers.Select(s => s.Name)));
            catalogText.AppendLine("CUSTOMERS: " + string.Join(", ", customers.Select(c => c.Name)));

            var tools = new object[]
            {
            new
            {
                name = "create_purchase_order",
                description = "Create a purchase order to restock inventory from a supplier",
                input_schema = new
                {
                    type = "object",
                    properties = new
                    {
                        supplierName = new { type = "string", description = "Must match a name from the SUPPLIERS list" },
                        items = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    productName = new { type = "string", description = "Must match a name from the PRODUCTS list" },
                                    quantity = new { type = "integer" },
                                    unitCost = new { type = "number" }
                                },
                                required = new[] { "productName", "quantity", "unitCost" }
                            }
                        }
                    },
                    required = new[] { "supplierName", "items" }
                }
            },
            new
            {
                name = "create_sales_order",
                description = "Create a sales order for a customer",
                input_schema = new
                {
                    type = "object",
                    properties = new
                    {
                        customerName = new { type = "string", description = "Must match a name from the CUSTOMERS list" },
                        items = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    productName = new { type = "string", description = "Must match a name from the PRODUCTS list" },
                                    quantity = new { type = "integer" }
                                },
                                required = new[] { "productName", "quantity" }
                            }
                        }
                    },
                    required = new[] { "customerName", "items" }
                }
            }
            };

            var prompt = $@"You are an ERP command assistant. Available catalog data:
{catalogText}

User command: ""{command}""

Determine which tool to call based on the user's intent, and extract the exact matching names from the catalog above. If a name in the command doesn't closely match anything in the catalog, still pick your best match from the list — do not invent new names.";

            var requestBody = new
            {
                model = "claude-sonnet-5",
                max_tokens = 1024,
                tools,
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
            var toolUseBlock = contentArray.EnumerateArray()
                .FirstOrDefault(b => b.GetProperty("type").GetString() == "tool_use");

            if (toolUseBlock.ValueKind == JsonValueKind.Undefined)
            {
                return new AiCommandProposal
                {
                    Success = false,
                    ErrorMessage = "I couldn't understand that as an order or purchase order command. Try something like: 'Create an order for Ali Khan, 2 Mouse'."
                };
            }

            var toolName = toolUseBlock.GetProperty("name").GetString()!;
            var input = toolUseBlock.GetProperty("input");

            if (toolName == "create_purchase_order")
                return BuildPurchaseOrderProposal(input, suppliers, products);

            if (toolName == "create_sales_order")
                return BuildSalesOrderProposal(input, customers, products);

            return new AiCommandProposal { Success = false, ErrorMessage = "Unrecognized action." };
        }

        private AiCommandProposal BuildPurchaseOrderProposal(JsonElement input, List<Domain.Entities.Supplier> suppliers, List<Domain.Entities.Product> products)
        {
            var supplierName = input.GetProperty("supplierName").GetString() ?? "";
            var supplier = suppliers.FirstOrDefault(s => s.Name.Equals(supplierName, StringComparison.OrdinalIgnoreCase))
                ?? suppliers.FirstOrDefault(s => s.Name.Contains(supplierName, StringComparison.OrdinalIgnoreCase));

            if (supplier == null)
                return new AiCommandProposal { Success = false, ErrorMessage = $"Could not find a supplier matching '{supplierName}'." };

            var items = new List<AiCommandItemProposal>();
            foreach (var item in input.GetProperty("items").EnumerateArray())
            {
                var productName = item.GetProperty("productName").GetString() ?? "";
                var product = products.FirstOrDefault(p => p.Name.Equals(productName, StringComparison.OrdinalIgnoreCase))
                    ?? products.FirstOrDefault(p => p.Name.Contains(productName, StringComparison.OrdinalIgnoreCase));

                if (product == null)
                    return new AiCommandProposal { Success = false, ErrorMessage = $"Could not find a product matching '{productName}'." };

                items.Add(new AiCommandItemProposal
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = item.GetProperty("quantity").GetInt32(),
                    UnitCost = item.GetProperty("unitCost").GetDecimal()
                });
            }

            return new AiCommandProposal
            {
                Success = true,
                ActionType = "create_purchase_order",
                SupplierId = supplier.Id,
                SupplierName = supplier.Name,
                Items = items,
                Summary = $"Purchase Order from {supplier.Name}: " + string.Join(", ", items.Select(i => $"{i.Quantity}x {i.ProductName} @ Rs.{i.UnitCost}"))
            };
        }

        private AiCommandProposal BuildSalesOrderProposal(JsonElement input, List<Domain.Entities.Customer> customers, List<Domain.Entities.Product> products)
        {
            var customerName = input.GetProperty("customerName").GetString() ?? "";
            var customer = customers.FirstOrDefault(c => c.Name.Equals(customerName, StringComparison.OrdinalIgnoreCase))
                ?? customers.FirstOrDefault(c => c.Name.Contains(customerName, StringComparison.OrdinalIgnoreCase));

            if (customer == null)
                return new AiCommandProposal { Success = false, ErrorMessage = $"Could not find a customer matching '{customerName}'." };

            var items = new List<AiCommandItemProposal>();
            foreach (var item in input.GetProperty("items").EnumerateArray())
            {
                var productName = item.GetProperty("productName").GetString() ?? "";
                var product = products.FirstOrDefault(p => p.Name.Equals(productName, StringComparison.OrdinalIgnoreCase))
                    ?? products.FirstOrDefault(p => p.Name.Contains(productName, StringComparison.OrdinalIgnoreCase));

                if (product == null)
                    return new AiCommandProposal { Success = false, ErrorMessage = $"Could not find a product matching '{productName}'." };

                items.Add(new AiCommandItemProposal
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = item.GetProperty("quantity").GetInt32()
                });
            }

            return new AiCommandProposal
            {
                Success = true,
                ActionType = "create_sales_order",
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                Items = items,
                Summary = $"Sales Order for {customer.Name}: " + string.Join(", ", items.Select(i => $"{i.Quantity}x {i.ProductName}"))
            };
        }
    }
}
