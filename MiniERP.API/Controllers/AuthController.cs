using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiniERP.Application.DTOs;
using MiniERP.Application.Interfaces;
using MiniERP.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;

namespace MiniERP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenService _jwtTokenService;

        public AuthController(UserManager<ApplicationUser> userManager, IJwtTokenService jwtTokenService)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            // Public signup: client ka bheja hua role ignore, hamesha Sales
            return await CreateUserAsync(request, "Sales");
        }

        [HttpPost("create-user")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateUser(RegisterRequest request)
        {
            var allowedRoles = new[] { "Admin", "Sales", "WarehouseStaff" };
            if (!allowedRoles.Contains(request.Role))
                return BadRequest(new[] { $"Invalid role '{request.Role}'." });

            return await CreateUserAsync(request, request.Role);
        }
        private async Task<IActionResult> CreateUserAsync(RegisterRequest request, string role)
        {
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description));

            await _userManager.AddToRoleAsync(user, role);
            return Ok(new { message = "User registered successfully." });
        }
        //[HttpPost("register")]
        //public async Task<IActionResult> Register(RegisterRequest request)
        //{
        //    var user = new ApplicationUser
        //    {
        //        UserName = request.Email,
        //        Email = request.Email,
        //        FullName = request.FullName
        //    };

        //    var result = await _userManager.CreateAsync(user, request.Password);
        //    if (!result.Succeeded)
        //        return BadRequest(result.Errors.Select(e => e.Description));

        //    await _userManager.AddToRoleAsync(user, request.Role);

        //    return Ok(new { message = "User registered successfully." });
        //}

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
                return Unauthorized(new { message = "Invalid email or password." });

            var roles = await _userManager.GetRolesAsync(user);
            var (token, expiresAt) = _jwtTokenService.GenerateToken(user.Id, user.Email!, roles);

            return Ok(new AuthResponse
            {
                Token = token,
                Email = user.Email!,
                FullName = user.FullName,
                Roles = roles,
                ExpiresAt = expiresAt
            });
        }
    }
}
