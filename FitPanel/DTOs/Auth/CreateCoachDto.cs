// DTOs/Auth/CreateCoachDto.cs
namespace FitPanel.DTOs.Auth;

public record CreateCoachDto(
    string FullName,
    string Email,
    string Password,
    string? Specialization,
    string? Bio
);