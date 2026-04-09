
namespace FitPanel.DTOs.Workout;

public class CreateExerciseDto
{
    public string ExerciseName { get; set; } = string.Empty;
    public string Sets { get; set; } = string.Empty;
    public string Reps { get; set; } = string.Empty;
    public string RestTime { get; set; } = string.Empty;
    public string? ExerciseLink { get; set; }
}
