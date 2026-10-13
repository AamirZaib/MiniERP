using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MiniERP.Application.DTOs;
using MiniERP.Application.Interfaces;
using MiniERP.Domain.Entities;

namespace MiniERP.Infrastructure.AI;

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
        var categories = (await _unitOfWork.Categories.GetAllAsync()).ToList();

        var catalogText = new StringBuilder();
        catalogText.AppendLine("PRODUCTS: " + string.Join(", ", products.Select(p => p.Name)));
        catalogText.AppendLine("SUPPLIERS: " + string.Join(", ", suppliers.Select(s => s.Name)));
        catalogText.AppendLine("CUSTOMERS: " + string.Join(", ", customers.Select(c => c.Name)));
        catalogText.AppendLine("CATEGORIES: " + string.Join(", ", categories.Select(c => c.Name)));

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
                        supplierName = new { type = "string" },
                        items = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    productName = new { type = "string" },
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
                description = "Create a sales order for an existing customer",
                input_schema = new
                {
                    type = "object",
                    properties = new
                    {
                        customerName = new { type = "string" },
                        items = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    productName = new { type = "string" },
                                    quantity = new { type = "integer" }
                                },
                                required = new[] { "productName", "quantity" }
                            }
                        }
                    },
                    required = new[] { "customerName", "items" }
                }
            },
            new
            {
                name = "create_customer",
                description = "Add a new customer record",
                input_schema = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string" },
                        email = new { type = "string" },
                        phone = new { type = "string" }
                    },
                    required = new[] { "name", "email" }
                }
            },
            new
            {
                name = "create_supplier",
                description = "Add a new supplier record",
                input_schema = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string" },
                        contactPerson = new { type = "string" },
                        email = new { type = "string" },
                        phone = new { type = "string" }
                    },
                    required = new[] { "name", "email" }
                }
            },
            new
            {
                name = "create_category",
                description = "Add a new product category",
                input_schema = new
                {
                    type = "object",
                    properties = new { name = new { type = "string" } },
                    required = new[] { "name" }
                }
            },
            new
            {
                name = "create_product",
                description = "Add a new product to the catalog",
                input_schema = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string" },
                        price = new { type = "number" },
                        stockQuantity = new { type = "integer" },
                        categoryName = new { type = "string" },
                        lowStockThreshold = new { type = "integer" }
                    },
                    required = new[] { "name", "price", "stockQuantity", "categoryName" }
                }
            }
        };

        var prompt = $@"You are an ERP command assistant. Existing data:
{catalogText}

User command: ""{command}""

Choose the tool that matches the user's intent.
Rules:
- For create_sales_order and create_purchase_order, customerName, supplierName and productName MUST be the closest matching existing names from the data above. Do not invent names.
- For create_customer, create_supplier, create_category and create_product, the new record's name is NEW. Use exactly what the user wrote.
- For create_product, categoryName must be an existing category from the list.
- Never invent values the user did not provide. If a required text value (such as an email) is missing, pass an empty string. If a required number is missing, pass 0.";

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

        var toolUseBlock = doc.RootElement.GetProperty("content").EnumerateArray()
            .FirstOrDefault(b => b.GetProperty("type").GetString() == "tool_use");

        if (toolUseBlock.ValueKind == JsonValueKind.Undefined)
        {
            return Fail("I couldn't understand that as a supported command. Try: 'Create an order for Ali Khan, 2 Mouse', 'Add customer Sara Ahmed, sara@example.com', or 'Add category Stationery'.");
        }

        var toolName = toolUseBlock.GetProperty("name").GetString()!;
        var input = toolUseBlock.GetProperty("input");

        return toolName switch
        {
            "create_purchase_order" => BuildPurchaseOrder(input, suppliers, products),
            "create_sales_order" => BuildSalesOrder(input, customers, products),
            "create_customer" => BuildCustomer(input, customers),
            "create_supplier" => BuildSupplier(input, suppliers),
            "create_category" => BuildCategory(input, categories),
            "create_product" => BuildProduct(input, products, categories),
            _ => Fail("Unrecognized action.")
        };
    }

    private static AiCommandProposal Fail(string message) => new() { Success = false, ErrorMessage = message };

    private static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()?.Trim() : null;

    private static decimal Num(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDecimal() : 0;

    private static AiCommandProposal BuildCustomer(JsonElement input, List<Customer> customers)
    {
        var name = Str(input, "name");
        var email = Str(input, "email");
        var phone = Str(input, "phone");

        if (string.IsNullOrWhiteSpace(name)) return Fail("Please include the customer's name.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return Fail("Please include a valid email address for the customer.");
        if (customers.Any(c => c.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            return Fail($"A customer with the email '{email}' already exists.");

        return new AiCommandProposal
        {
            Success = true,
            ActionType = "create_customer",
            Name = name,
            Email = email,
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone,
            Summary = $"New customer: {name} ({email})" + (string.IsNullOrWhiteSpace(phone) ? "" : $", {phone}")
        };
    }

    private static AiCommandProposal BuildSupplier(JsonElement input, List<Supplier> suppliers)
    {
        var name = Str(input, "name");
        var email = Str(input, "email");
        var contact = Str(input, "contactPerson");
        var phone = Str(input, "phone");

        if (string.IsNullOrWhiteSpace(name)) return Fail("Please include the supplier's name.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return Fail("Please include a valid email address for the supplier.");
        if (suppliers.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return Fail($"A supplier named '{name}' already exists.");

        return new AiCommandProposal
        {
            Success = true,
            ActionType = "create_supplier",
            Name = name,
            Email = email,
            ContactPerson = string.IsNullOrWhiteSpace(contact) ? null : contact,
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone,
            Summary = $"New supplier: {name} ({email})" + (string.IsNullOrWhiteSpace(contact) ? "" : $", contact: {contact}")
        };
    }

    private static AiCommandProposal BuildCategory(JsonElement input, List<Category> categories)
    {
        var name = Str(input, "name");
        if (string.IsNullOrWhiteSpace(name)) return Fail("Please include the category name.");
        if (categories.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return Fail($"A category named '{name}' already exists.");

        return new AiCommandProposal
        {
            Success = true,
            ActionType = "create_category",
            Name = name,
            Summary = $"New category: {name}"
        };
    }

    private static AiCommandProposal BuildProduct(JsonElement input, List<Product> products, List<Category> categories)
    {
        var name = Str(input, "name");
        var price = Num(input, "price");
        var stock = (int)Num(input, "stockQuantity");
        var threshold = (int)Num(input, "lowStockThreshold");
        var categoryName = Str(input, "categoryName") ?? "";

        if (string.IsNullOrWhiteSpace(name)) return Fail("Please include the product name.");
        if (price <= 0) return Fail("Please include a price greater than zero.");
        if (stock < 0) return Fail("Stock quantity cannot be negative.");
        if (products.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return Fail($"A product named '{name}' already exists.");

        var category = categories.FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase))
            ?? categories.FirstOrDefault(c => c.Name.Contains(categoryName, StringComparison.OrdinalIgnoreCase));
        if (category == null) return Fail($"Could not find a category matching '{categoryName}'. Create the category first.");

        return new AiCommandProposal
        {
            Success = true,
            ActionType = "create_product",
            Name = name,
            Price = price,
            StockQuantity = stock,
            LowStockThreshold = threshold > 0 ? threshold : 10,
            CategoryId = category.Id,
            CategoryName = category.Name,
            Summary = $"New product: {name} in {category.Name}, Price Rs.{price:N0}, Stock {stock}"
        };
    }

    private static AiCommandProposal BuildPurchaseOrder(JsonElement input, List<Supplier> suppliers, List<Product> products)
    {
        var supplierName = Str(input, "supplierName") ?? "";
        var supplier = suppliers.FirstOrDefault(s => s.Name.Equals(supplierName, StringComparison.OrdinalIgnoreCase))
            ?? suppliers.FirstOrDefault(s => s.Name.Contains(supplierName, StringComparison.OrdinalIgnoreCase));
        if (supplier == null) return Fail($"Could not find a supplier matching '{supplierName}'.");

        var items = new List<AiCommandItemProposal>();
        foreach (var item in input.GetProperty("items").EnumerateArray())
        {
            var productName = Str(item, "productName") ?? "";
            var product = products.FirstOrDefault(p => p.Name.Equals(productName, StringComparison.OrdinalIgnoreCase))
                ?? products.FirstOrDefault(p => p.Name.Contains(productName, StringComparison.OrdinalIgnoreCase));
            if (product == null) return Fail($"Could not find a product matching '{productName}'.");

            items.Add(new AiCommandItemProposal
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = (int)Num(item, "quantity"),
                UnitCost = Num(item, "unitCost")
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

    private static AiCommandProposal BuildSalesOrder(JsonElement input, List<Customer> customers, List<Product> products)
    {
        var customerName = Str(input, "customerName") ?? "";
        var customer = customers.FirstOrDefault(c => c.Name.Equals(customerName, StringComparison.OrdinalIgnoreCase))
            ?? customers.FirstOrDefault(c => c.Name.Contains(customerName, StringComparison.OrdinalIgnoreCase));
        if (customer == null) return Fail($"Could not find a customer matching '{customerName}'.");

        var items = new List<AiCommandItemProposal>();
        foreach (var item in input.GetProperty("items").EnumerateArray())
        {
            var productName = Str(item, "productName") ?? "";
            var product = products.FirstOrDefault(p => p.Name.Equals(productName, StringComparison.OrdinalIgnoreCase))
                ?? products.FirstOrDefault(p => p.Name.Contains(productName, StringComparison.OrdinalIgnoreCase));
            if (product == null) return Fail($"Could not find a product matching '{productName}'.");

            items.Add(new AiCommandItemProposal
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = (int)Num(item, "quantity")
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