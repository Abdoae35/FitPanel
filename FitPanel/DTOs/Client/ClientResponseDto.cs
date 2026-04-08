
namespace FitPanel.DTOs.Client;

   public record ClientResponseDto(
    int Id,
    string Name,
    double Weight,
    int SubscriptionDurationPerMonth,
    string? InbodyLink,
    string? FromPicLink,
    string? ToPicLink,
    DateTime CreatedAt,
    string CoachName
);
