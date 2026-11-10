using MarketingApi.Common;
using MarketingApi.DTOs.Auth;
using MarketingApi.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketingApi.Controllers;

/// <summary>
/// API Controller for Users and Roles.
/// Accessible via /api/Users.
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
        var user = await _usersService.AuthenticateAsync(request.Username, request.Password);
        if (user == null)
        {
            return Problem(
                title: "Authentication Failed",
                detail: "Invalid username or password.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var token = _jwtTokenService.GenerateToken(user);
        return Ok(new AuthTokenResponseDto
        {
            Token = token,
            Username = user.Username,
            Name = user.Name ?? user.Username,
            Role = AppRoles.Normalize(user.Role?.RoleName)
        });
    }

    /// <summary>
    /// Returns all system user roles.
    /// GET /api/Users/roles
    /// </summary>
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _usersService.GetRoles();
        return Ok(roles);
    }
}
