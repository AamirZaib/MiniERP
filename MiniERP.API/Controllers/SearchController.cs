using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniERP.Application.Interfaces;
using System.Security.Claims;

namespace MiniERP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SearchController : ControllerBase
    {
        private readonly IGlobalSearchService _globalSearchService;
        private readonly IRateLimitService _rateLimitService;

        public SearchController(IGlobalSearchService globalSearchService, IRateLimitService rateLimitService)
        {
            _globalSearchService = globalSearchService;
            _rateLimitService = rateLimitService;
        }

        [HttpGet("global")]
        public async Task<IActionResult> Global([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { message = "Query is required." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            if (await _rateLimitService.IsGlobalLimitReachedAsync(globalDailyLimit: 100))
                return StatusCode(429, new { message = "AI search is temporarily unavailable due to high demand. Please try again tomorrow." });

            if (!await _rateLimitService.CanSearchAsync(userId, dailyLimit: 5))
                return StatusCode(429, new { message = "You've reached your daily AI search limit (5/day). Please try again tomorrow." });

            var result = await _globalSearchService.SearchAsync(query);
            await _rateLimitService.LogSearchAsync(userId);

            return Ok(result);
        }
    }
}
