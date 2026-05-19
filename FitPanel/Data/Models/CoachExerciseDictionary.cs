using System;
using System.ComponentModel.DataAnnotations;

namespace FitPanel.Data.Models;

public class CoachExerciseDictionary
{
    public int Id { get; set; }

    // This ensures every coach has their OWN separated data
    [Required]
    public string CoachId { get; set; }
    public PanelUser Coach { get; set; }

    [Required]
    public string ExerciseName { get; set; }
    
    public string? ExcerciseLink { get; set; }
}
