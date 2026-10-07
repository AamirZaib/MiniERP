using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniERP.Application.DTOs;
using MiniERP.Application.Interfaces;
using MiniERP.Domain.Exceptions;
using System.Security.Claims;

namespace MiniERP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Sales,WarehouseStaff")]
    public class AiCommandController : ControllerBase
    {
        private readonly IAiCommandService _aiCommandService;
        private readonly IRateLimitService _rateLimitService;
        private readonly IOrderService _orderService;
        private readonly IPurchaseOrderService _purchaseOrderService;

        public AiCommandController(
            IAiCommandService aiCommandService,
            IRateLimitService rateLimitService,
            IOrderService orderService,
            IPurchaseOrderService purchaseOrderService)
        {
            _aiCommandService = aiCommandService;
            _rateLimitService = rateLimitService;
            _orderService = orderService;
            _purchaseOrderService = purchaseOrderService;
        }

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

            return Ok(proposal);
        }

        [HttpPost("execute")]
        public async Task<IActionResult> Execute(AiCommandProposal proposal)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var userEmail = User.FindFirstValue(ClaimTypes.Email)!;

            try
            {
                if (proposal.ActionType == "create_sales_order")
                {
                    var request = new CreateOrderRequest
                    {
                        CustomerId = proposal.CustomerId!.Value,
                        Items = proposal.Items.Select(i => new CreateOrderItemRequest { ProductId = i.ProductId, Quantity = i.Quantity }).ToList()
                    };
                    var order = await _orderService.CreateOrderAsync(request, userId, userEmail);
                    return Ok(new { message = $"Order #{order.Id} created successfully." });
                }

                if (proposal.ActionType == "create_purchase_order")
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

                return BadRequest(new { message = "Unknown action type." });
            }
            catch (InsufficientStockException ex) { return BadRequest(new { message = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}
