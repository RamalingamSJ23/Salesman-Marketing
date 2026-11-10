using MarketingApi.Service;
using MarketingApi.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketingApi.Controllers
{
    /// <summary>
    /// API Controller for Users and Roles.
    /// Accessible via /api/users and /api/Users.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUsersService _usersService;
        private readonly IJwtTokenService _jwtTokenService;

        public UsersController(IUsersService usersService, IJwtTokenService jwtTokenService)
        {
            _usersService = usersService;
            _jwtTokenService = jwtTokenService;
        }

        [AllowAnonymous]  
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _usersService.AuthenticateAsync(request.Username, request.Password);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid username or password." });
            }

            var token = _jwtTokenService.GenerateToken(user);
            return Ok(new AuthTokenResponseDto
            {
                Token = token,
                Username = user.Username,
                Name = user.Name ?? user.Username,
                Role = user.Role?.Rolename ?? "User"
            });
        }

        /// <summary>
        /// Returns all system user roles.
        /// GET /api/users/roles
        /// </summary>
        [Authorize]
        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _usersService.GetRoles();
            return Ok(roles);
        }
    }
}
