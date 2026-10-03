using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniERP.Application.DTOs;
using MiniERP.Application.Interfaces;
using MiniERP.Domain.Entities;
using MiniERP.Infrastructure.Services;
using System.Security.Claims;

namespace MiniERP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAiSearchService _aiSearchService;
        private readonly IRateLimitService _rateLimitService;
        private readonly IAuditService _auditService;

        public ProductsController(IUnitOfWork unitOfWork, IAiSearchService aiSearchService, IRateLimitService rateLimitService, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _aiSearchService = aiSearchService;
            _rateLimitService = rateLimitService; 
            _auditService = auditService;

        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _unitOfWork.Products.GetAllWithIncludesAsync(p => p.Category);
            var response = products.Select(MapToResponse);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _unitOfWork.Products.GetByIdWithIncludesAsync(id, p => p.Category);
            if (product == null) return NotFound();
            return Ok(MapToResponse(product));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,WarehouseStaff")]
        public async Task<IActionResult> Create(CreateProductRequest request)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId);
            if (category == null)
                return BadRequest(new { message = $"Category with Id {request.CategoryId} not found." });

            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                StockQuantity = request.StockQuantity,
                LowStockThreshold = request.LowStockThreshold,
                CategoryId = request.CategoryId
            };

            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var userEmail = User.FindFirstValue(ClaimTypes.Email)!;

            await _auditService.LogAsync(userId, userEmail, "ProductCreated", "Product", product.Id.ToString(),
                $"Product '{product.Name}' created, Price: Rs.{product.Price}, Stock: {product.StockQuantity}");
            // category.Name yahan available hai kyunki hum ne category already load ki thi
            var response = new ProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                LowStockThreshold = product.LowStockThreshold,
                IsLowStock = product.IsLowStock,
                CategoryId = product.CategoryId,
                CategoryName = category.Name
            };

            return CreatedAtAction(nameof(GetById), new { id = product.Id }, response);
        }

        [HttpGet("smart-search")]
        public async Task<IActionResult> SmartSearch([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { message = "Query is required." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // Global safety net pehle check karein (sabse zaroori)
            if (await _rateLimitService.IsGlobalLimitReachedAsync(globalDailyLimit: 100))
                return StatusCode(429, new { message = "AI search is temporarily unavailable due to high demand. Please try again tomorrow." });

            // Per-user limit
            if (!await _rateLimitService.CanSearchAsync(userId, dailyLimit: 5))
                return StatusCode(429, new { message = "You've reached your daily AI search limit (5/day). Please try again tomorrow." });

            var result = await _aiSearchService.SearchAsync(query);
            await _rateLimitService.LogSearchAsync(userId);

            return Ok(result);
        }
        private static ProductResponse MapToResponse(Product product)
        {
            return new ProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                LowStockThreshold = product.LowStockThreshold,
                IsLowStock = product.IsLowStock,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name ?? string.Empty
            };
        }
        // ProductsController.cs mein add karein
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,WarehouseStaff")]
        public async Task<IActionResult> Update(int id, UpdateProductRequest request)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null) return NotFound();

            var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId);
            if (category == null)
                return BadRequest(new { message = $"Category with Id {request.CategoryId} not found." });

            product.Name = request.Name;
            product.Description = request.Description;
            product.Price = request.Price;
            product.StockQuantity = request.StockQuantity;
            product.LowStockThreshold = request.LowStockThreshold;
            product.CategoryId = request.CategoryId;

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var userEmail = User.FindFirstValue(ClaimTypes.Email)!;
            await _auditService.LogAsync(userId, userEmail, "ProductUpdated", "Product", id.ToString(),
                $"Product '{product.Name}' updated");

            return NoContent();
        }

    }
}
