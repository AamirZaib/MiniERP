using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniERP.Application.Interfaces;

namespace MiniERP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AuditLogsController : ControllerBase
    {
        private readonly IAuditService _auditService;
        public AuditLogsController(IAuditService auditService) => _auditService = auditService;

        [HttpGet]
        public async Task<IActionResult> GetRecent([FromQuery] int count = 50)
        {
            var logs = await _auditService.GetRecentAsync(count);
            return Ok(logs);
        }
    }
}
