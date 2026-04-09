// DTOs/Auth/CreateCoachDto.cs
namespace FitPanel.DTOs.Auth;

public class CreateCoachDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Specialization { get; set; }
    public string? Bio { get; set; }
}