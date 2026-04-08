
namespace FitPanel.DTOs.Workout;

   public record CardioResponseDto(
    int Id,
    string CardioType,
    int DurationMinutes,
    CardioIntensity Intensity,
    string? Notes
);
