using System;

namespace FitPanel.Data.Models;

public class DietTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CoachId { get; set; } = string.Empty;
    public string DietJson { get; set; } = string.Empty; // Serialized JSON containing list of CreateDietMealDto or full nested meal models
    public string? Instructions { get; set; }
    public int NumberOfMeals { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
