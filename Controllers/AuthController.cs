using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using NordShop.Api.Data;
using NordShop.Api.Models;
using NordShop.Api.Services;

namespace NordShop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    ShopDbContext db,
    IPasswordHasher<DemoUser> hasher,
    TokenService tokenService) : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var user = await db.Users.FirstOrDefaultAsync(
            x => x.Username.ToLower() == username.ToLower(), cancellationToken);

        if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
            == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Неверный логин или пароль." });
        }

        return Ok(new LoginResponse(
            tokenService.CreateToken(user),
            new UserResponse(user.Id, user.Username, user.DisplayName, user.Role)));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken cancellationToken)
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idValue, out var id)) return Unauthorized();

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return user is null
            ? Unauthorized()
            : Ok(new UserResponse(user.Id, user.Username, user.DisplayName, user.Role));
    }
}
