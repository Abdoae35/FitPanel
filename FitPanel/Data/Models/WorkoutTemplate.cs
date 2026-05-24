using System;

namespace FitPanel.Data.Models;

public class WorkoutTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CoachId { get; set; } = string.Empty;
    public string WorkoutJson { get; set; } = string.Empty; // Serialized JSON containing list of WorkOutDay templates
    public string SplitName { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public int NumberOfWorkoutDays { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
