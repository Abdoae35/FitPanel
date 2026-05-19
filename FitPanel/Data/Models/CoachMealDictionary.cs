using System;
using System.ComponentModel.DataAnnotations;

namespace FitPanel.Data.Models;

public class CoachMealDictionary
{
    public int Id { get; set; }

    // This ensures every coach has their OWN separated data
    [Required]
    public string CoachId { get; set; }
    public PanelUser Coach { get; set; }

    [Required]
    public string MealName { get; set; }

    public string? Link { get; set; }

    public int Protein { get; set; }
    public int Carbs { get; set; }
    public int Fats { get; set; }
    public int Calories { get; set; }
}
