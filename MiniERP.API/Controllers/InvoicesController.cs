using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniERP.Application.Interfaces;

namespace MiniERP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        public InvoicesController(IInvoiceService invoiceService) => _invoiceService = invoiceService;

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _invoiceService.GetAllAsync());

        [HttpPost("generate/{orderId}")]
        [Authorize(Roles = "Admin,Sales")]
        public async Task<IActionResult> Generate(int orderId)
        {
            try
            {
                var result = await _invoiceService.GenerateAsync(orderId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpGet("{id}/pdf")]
        public async Task<IActionResult> DownloadPdf(int id)
        {
            var pdfBytes = await _invoiceService.GeneratePdfAsync(id);
            return File(pdfBytes, "application/pdf", $"invoice-{id}.pdf");
        }
    }
}
