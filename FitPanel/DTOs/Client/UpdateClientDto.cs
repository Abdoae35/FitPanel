
namespace FitPanel.DTOs.Client;

public class UpdateClientDto
{
    public string? Name { get; set; }
    public double? Weight { get; set; }
    public int? SubscriptionDurationPerMonth { get; set; }
    public string? InbodyLink { get; set; }
    public string? FromPicLink { get; set; }
    public string? ToPicLink { get; set; }
}