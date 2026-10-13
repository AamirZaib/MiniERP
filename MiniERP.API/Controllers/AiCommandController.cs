using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniERP.Application.DTOs;
using MiniERP.Application.Interfaces;
using MiniERP.Domain.Entities;
using MiniERP.Domain.Exceptions;
using System.Security.Claims;

namespace MiniERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Sales,WarehouseStaff")]
public class AiCommandController : ControllerBase
{
    private readonly IAiCommandService _aiCommandService;
    private readonly IRateLimitService _rateLimitService;
    private readonly IOrderService _orderService;
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;

    public AiCommandController(
        IAiCommandService aiCommandService,
        IRateLimitService rateLimitService,
        IOrderService orderService,
        IPurchaseOrderService purchaseOrderService,
        IUnitOfWork unitOfWork,
        IAuditService auditService)
    {
        _aiCommandService = aiCommandService;
        _rateLimitService = rateLimitService;
        _orderService = orderService;
        _purchaseOrderService = purchaseOrderService;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }

    // Har action ke liye wahi roles jo normal pages/endpoints par hain
    private bool CanPerform(string actionType) => actionType switch
    {
        "create_sales_order" or "create_customer" => User.IsInRole("Admin") || User.IsInRole("Sales"),
        "create_purchase_order" or "create_supplier" or "create_product" => User.IsInRole("Admin") || User.IsInRole("WarehouseStaff"),
        "create_category" => User.IsInRole("Admin"),
        _ => false
    };

    [HttpPost("parse")]
    public async Task<IActionResult> Parse(AiCommandRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Command))
            return BadRequest(new { message = "Command is required." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (await _rateLimitService.IsGlobalLimitReachedAsync(globalDailyLimit: 100))
            return StatusCode(429, new { message = "AI features are temporarily unavailable due to high demand." });

        if (!await _rateLimitService.CanSearchAsync(userId, dailyLimit: 5))
            return StatusCode(429, new { message = "You've reached your daily AI usage limit (5/day)." });

        var proposal = await _aiCommandService.ParseAsync(request.Command);
        await _rateLimitService.LogSearchAsync(userId);

        if (proposal.Success && !CanPerform(proposal.ActionType))
            proposal = new AiCommandProposal { Success = false, ErrorMessage = "Your role is not allowed to perform this action." };

        return Ok(proposal);
    }

    [HttpPost("execute")]
    public async Task<IActionResult> Execute(AiCommandProposal proposal)
    {
        // Proposal browser se wapas aata hai, is liye role aur data dono dobara check hote hain
        if (!CanPerform(proposal.ActionType))
            return StatusCode(403, new { message = "Your role is not allowed to perform this action." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userEmail = User.FindFirstValue(ClaimTypes.Email)!;

        try
        {
            switch (proposal.ActionType)
            {
                case "create_sales_order":
                    {
                        var request = new CreateOrderRequest
                        {
                            CustomerId = proposal.CustomerId!.Value,
                            Items = proposal.Items.Select(i => new CreateOrderItemRequest { ProductId = i.ProductId, Quantity = i.Quantity }).ToList()
                        };
                        var order = await _orderService.CreateOrderAsync(request, userId, userEmail);
                        return Ok(new { message = $"Order #{order.Id} created successfully." });
                    }

                case "create_purchase_order":
                    {
                        var request = new CreatePurchaseOrderRequest
                        {
                            SupplierId = proposal.SupplierId!.Value,
                            Items = proposal.Items.Select(i => new CreatePurchaseOrderItemRequest
                            {
                                ProductId = i.ProductId,
                                Quantity = i.Quantity,
                                UnitCost = i.UnitCost ?? 0
                            }).ToList()
                        };
                        var po = await _purchaseOrderService.CreateAsync(request, userId, userEmail);
                        return Ok(new { message = $"Purchase Order #{po.Id} created successfully." });
                    }

                case "create_customer":
                    {
                        if (string.IsNullOrWhiteSpace(proposal.Name) || string.IsNullOrWhiteSpace(proposal.Email))
                            return BadRequest(new { message = "Name and email are required." });

                        var customer = new Customer { Name = proposal.Name, Email = proposal.Email, Phone = proposal.Phone };
                        await _unitOfWork.Customers.AddAsync(customer);
                        await _unitOfWork.SaveChangesAsync();
                        await _auditService.LogAsync(userId, userEmail, "CustomerCreated", "Customer", customer.Id.ToString(),
                            $"Customer '{customer.Name}' created via AI Command");
                        return Ok(new { message = $"Customer '{customer.Name}' created successfully." });
                    }

                case "create_supplier":
                    {
                        if (string.IsNullOrWhiteSpace(proposal.Name) || string.IsNullOrWhiteSpace(proposal.Email))
                            return BadRequest(new { message = "Name and email are required." });

                        var supplier = new Supplier
                        {
                            Name = proposal.Name,
                            Email = proposal.Email,
                            ContactPerson = proposal.ContactPerson,
                            Phone = proposal.Phone
                        };
                        await _unitOfWork.Suppliers.AddAsync(supplier);
                        await _unitOfWork.SaveChangesAsync();
                        await _auditService.LogAsync(userId, userEmail, "SupplierCreated", "Supplier", supplier.Id.ToString(),
                            $"Supplier '{supplier.Name}' created via AI Command");
                        return Ok(new { message = $"Supplier '{supplier.Name}' created successfully." });
                    }

                case "create_category":
                    {
                        if (string.IsNullOrWhiteSpace(proposal.Name))
                            return BadRequest(new { message = "Name is required." });

                        var category = new Category { Name = proposal.Name };
                        await _unitOfWork.Categories.AddAsync(category);
                        await _unitOfWork.SaveChangesAsync();
                        await _auditService.LogAsync(userId, userEmail, "CategoryCreated", "Category", category.Id.ToString(),
                            $"Category '{category.Name}' created via AI Command");
                        return Ok(new { message = $"Category '{category.Name}' created successfully." });
                    }

                case "create_product":
                    {
                        if (string.IsNullOrWhiteSpace(proposal.Name) || (proposal.Price ?? 0) <= 0 || (proposal.StockQuantity ?? -1) < 0)
                            return BadRequest(new { message = "Valid name, price and stock are required." });

                        var category = await _unitOfWork.Categories.GetByIdAsync(proposal.CategoryId ?? 0);
                        if (category == null)
                            return BadRequest(new { message = "Category not found." });

                        var product = new Product
                        {
                            Name = proposal.Name,
                            Price = proposal.Price!.Value,
                            StockQuantity = proposal.StockQuantity!.Value,
                            LowStockThreshold = proposal.LowStockThreshold ?? 10,
                            CategoryId = category.Id
                        };
                        await _unitOfWork.Products.AddAsync(product);
                        await _unitOfWork.SaveChangesAsync();
                        await _auditService.LogAsync(userId, userEmail, "ProductCreated", "Product", product.Id.ToString(),
                            $"Product '{product.Name}' created via AI Command, Price: Rs.{product.Price}, Stock: {product.StockQuantity}");
                        return Ok(new { message = $"Product '{product.Name}' created successfully." });
                    }

                default:
                    return BadRequest(new { message = "Unknown action type." });
            }
        }
        catch (InsufficientStockException ex) { return BadRequest(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }
}