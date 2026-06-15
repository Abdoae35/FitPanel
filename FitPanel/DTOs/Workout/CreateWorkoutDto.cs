// DTOs/Workout/CreateWorkoutDto.cs
using FitPanel.Data.Models;

namespace FitPanel.DTOs.Workout;

public class CreateWorkoutDto
{
    public string SplitName { get; set; } = string.Empty;
    public int NumberOfWorkoutDays { get; set; } = 3;
    // FIX #5: Instructions was in the model but never set from DTO
    public string? Instructions { get; set; }
}





public class CreateCardioDto
{
    public string CardioType { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public CardioIntensity Intensity { get; set; } = CardioIntensity.Medium;
    public string? Notes { get; set; }
}

public record ExerciseResponseDto(
    int Id,
    string ExerciseName,
    string Sets,
    string Reps,
    string RestTime,
    string? ExerciseLink
);

public record CardioResponseDto(
    int Id,
    string CardioType,
    int DurationMinutes,
    CardioIntensity Intensity,
    string? Notes
);

public class WorkoutDayResponseDto
{
    public int Id { get; set; }
    public string DayName { get; set; } = string.Empty;
    public Days Day { get; set; }
    public List<ExerciseResponseDto> Exercises { get; set; } = new();
    public CardioResponseDto? Cardio { get; set; }
    public string? Notes { get; set; }
    public string? WarmUpName { get; set; }
    public string? WarmUpLink { get; set; }

    public WorkoutDayResponseDto() { }
    public WorkoutDayResponseDto(int id, string dayName, Days day,
        List<ExerciseResponseDto> exercises, CardioResponseDto? cardio, string? notes = null,
        string? warmUpName = null, string? warmUpLink = null)
    {
        Id = id; DayName = dayName; Day = day;
        Exercises = exercises; Cardio = cardio; Notes = notes;
        WarmUpName = warmUpName; WarmUpLink = warmUpLink;
    }
}

public class WorkoutResponseDto
{
    public int Id { get; set; }
    public string SplitName { get; set; } = string.Empty;
    public int NumberOfWorkoutDays { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<WorkoutDayResponseDto> WorkoutDays { get; set; } = new();

    public WorkoutResponseDto() { }
    public WorkoutResponseDto(int id, string splitName, int numberOfWorkoutDays,
        DateTime createdAt, List<WorkoutDayResponseDto> workoutDays)
    {
        Id = id; SplitName = splitName; NumberOfWorkoutDays = numberOfWorkoutDays;
        CreatedAt = createdAt; WorkoutDays = workoutDays;
    }
}
