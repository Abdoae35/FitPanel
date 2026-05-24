// DTOs/Client/ClientResponseDto.cs
namespace FitPanel.DTOs.Client;

public record ClientResponseDto(
    int Id,
    string Name,
    double Weight,
    double Bmr,
    int SubscriptionDurationPerMonth,
    string? InbodyLink,
    string? FromPicLink,
    string? ToPicLink,
    DateTime CreatedAt,
    string CoachName,
    DateTime StartDate,
    DateTime EndDate,
    string? Goal
);