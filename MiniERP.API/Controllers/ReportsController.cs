using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniERP.Application.Interfaces;

namespace MiniERP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // reports sirf Admin dekh sakta hai
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("sales-by-month")]
        public async Task<IActionResult> GetSalesByMonth()
        {
            var report = await _reportService.GetSalesByMonthAsync();
            return Ok(report);
        }

        [HttpGet("top-products")]
        public async Task<IActionResult> GetTopProducts([FromQuery] int topN = 5)
        {
            var report = await _reportService.GetTopProductsAsync(topN);
            return Ok(report);
        }
    }
}
