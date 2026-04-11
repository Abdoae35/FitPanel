namespace FitPanel.DTOs.Diet;

public class CreateAlternativeDto
{
    public string MealName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Protein { get; set; }
    public int Carbs { get; set; }
    public int Fats { get; set; }
    public int Calories { get; set; }
    public string? Link { get; set; }
}