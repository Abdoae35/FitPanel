// Services/IAuthService.cs
using FitPanel.DTOs.Auth;

namespace FitPanel.Services;

public interface IAuthService
{
    Task<(bool Success, string Message)> CreateCoachAsync(CreateCoachDto dto);
}