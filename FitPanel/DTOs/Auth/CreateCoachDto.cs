// DTOs/Auth/CreateCoachDto.cs
namespace FitPanel.DTOs.Auth;

public class CreateCoachDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Specialization { get; set; }
    public string? Bio { get; set; }
    
    public string? InstagramUsername { get; set; }
    public string? InstagramLink { get; set; }
    public string? PhoneNumber { get; set; }
    public int MaxClients { get; set; } = 10;
}