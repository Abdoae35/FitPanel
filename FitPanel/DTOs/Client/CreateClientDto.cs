namespace FitPanel.DTOs.Client;

public record CreateClientDto(
    string Name,
    double Weight,
    int SubscriptionDurationPerMonth,
    string? InbodyLink,
    string? FromPicLink,
    string? ToPicLink
);