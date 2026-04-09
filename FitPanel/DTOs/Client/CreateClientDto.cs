namespace FitPanel.DTOs.Client;

public class CreateClientDto
{
    public string Name { get; set; } = string.Empty;
    public double Weight { get; set; }
    public int SubscriptionDurationPerMonth { get; set; }
    public string? InbodyLink { get; set; }
    public string? FromPicLink { get; set; }
    public string? ToPicLink { get; set; }
}