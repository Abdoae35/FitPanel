// Services/AuthService.cs
using FitPanel.Data;
using FitPanel.DTOs.Auth;
using Microsoft.AspNetCore.Identity;

namespace FitPanel.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<PanelUser> _userManager;

    public AuthService(UserManager<PanelUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<(bool Success, string Message)> CreateCoachAsync(CreateCoachDto dto)
    {
        var existing = await _userManager.FindByEmailAsync(dto.Email);
        if (existing != null)
            return (false, "Email already exists.");

        var coach = new PanelUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName,
            Specialization = dto.Specialization,
            Bio = dto.Bio,
            EmailConfirmed = true,
            IsActive = true,
            PhoneNumber = dto.PhoneNumber,
            InstagramUsername = dto.InstagramUsername,
            InstagramLink = dto.InstagramLink,
            MaxClients = dto.MaxClients,
            SubscriptionEndDate = DateTime.UtcNow.AddYears(1)
        };

        var result = await _userManager.CreateAsync(coach, dto.Password);
        if (!result.Succeeded)
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(coach, "Coach");
        return (true, "Coach created successfully.");
    }
}