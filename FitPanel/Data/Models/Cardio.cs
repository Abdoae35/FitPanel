// Data/Models/Cardio.cs
namespace FitPanel.Data.Models;

public class Cardio
{
    public int Id { get; set; }
    public string CardioType { get; set; } = string.Empty; // Treadmill, Bike, Rowing...
    public int DurationMinutes { get; set; }
    public string? Notes { get; set; }                     // "After session", "Fasted"
    public CardioIntensity Intensity { get; set; }         // Low, Medium, High

    // FK → WorkOutDay (one to one, optional)
    public int WorkOutDayId { get; set; }
    public WorkOutDay WorkOutDay { get; set; } = null!;
}

