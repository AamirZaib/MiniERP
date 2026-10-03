using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniERP.Application.DTOs;
using MiniERP.Application.Interfaces;
using System.Security.Claims;

namespace MiniERP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly IPurchaseOrderService _purchaseOrderService;
        public PurchaseOrdersController(IPurchaseOrderService purchaseOrderService) => _purchaseOrderService = purchaseOrderService;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _purchaseOrderService.GetAllAsync();
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,WarehouseStaff")]
        public async Task<IActionResult> Create(CreatePurchaseOrderRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var userEmail = User.FindFirstValue(ClaimTypes.Email)!;
            var result = await _purchaseOrderService.CreateAsync(request, userId, userEmail);
            return Ok(result);
        }
        [HttpPatch("{id}/receive")]
        [Authorize(Roles = "Admin,WarehouseStaff")]
        public async Task<IActionResult> Receive(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var userEmail = User.FindFirstValue(ClaimTypes.Email)!;
                await _purchaseOrderService.MarkAsReceivedAsync(id, userId, userEmail);
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}
