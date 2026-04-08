
namespace FitPanel.DTOs.Client;

   public record UpdateClientDto(
    string? Name,
    double? Weight,
    int? SubscriptionDurationPerMonth,
    string? InbodyLink,
    string? FromPicLink,
    string? ToPicLink
);