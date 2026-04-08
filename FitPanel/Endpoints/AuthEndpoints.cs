// Endpoints/AuthEndpoints.cs
using FitPanel.DTOs.Auth;
using FitPanel.Services;
using Microsoft.AspNetCore.Authorization;

namespace FitPanel.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

group.MapPost("/login", async (
    LoginDto dto,
    SignInManager<PanelUser> signInManager,
    UserManager<PanelUser> userManager) =>
{
    var user = await userManager.FindByEmailAsync(dto.Email);
    if (user == null || !user.IsActive)
        return Results.Unauthorized();

    var result = await signInManager.PasswordSignInAsync(user, dto.Password, false, lockoutOnFailure: true);
    return result.Succeeded
        ? Results.Ok(new { message = "Logged in successfully." })
        : Results.Unauthorized();
});
        // Admin only — create coach account
        group.MapPost("/create-coach", async (
            CreateCoachDto dto,
            IAuthService authService) =>
        {
            var (success, message) = await authService.CreateCoachAsync(dto);
            return success
                ? Results.Ok(new { message })
                : Results.BadRequest(new { message });
        })
        .RequireAuthorization("AdminOnly");
    }
}