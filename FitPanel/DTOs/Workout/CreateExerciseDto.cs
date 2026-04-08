
namespace FitPanel.DTOs.Workout;

   public record CreateExerciseDto(
    string ExerciseName,
    string Sets,
    string Reps,
    string RestTime,
    string? ExerciseLink
);
