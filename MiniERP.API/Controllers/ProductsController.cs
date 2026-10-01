using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniERP.Application.DTOs;
using MiniERP.Application.Interfaces;
using MiniERP.Domain.Entities;

namespace MiniERP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAiSearchService _aiSearchService;

        public ProductsController(IUnitOfWork unitOfWork, IAiSearchService aiSearchService)
        {
            _unitOfWork = unitOfWork;
            _aiSearchService = aiSearchService;
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

            var result = await _aiSearchService.SearchAsync(query);
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

    }
}
