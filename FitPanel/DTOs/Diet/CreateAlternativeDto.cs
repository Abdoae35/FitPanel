namespace FitPanel.DTOs.Diet;

// AlternativeItem is an ingredient-level swap — no Link
public class CreateAlternativeDto
{
    public string MealName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public string? Unit { get; set; }
    public int Protein { get; set; }
    public int Carbs { get; set; }
    public int Fats { get; set; }
    public int Calories { get; set; }
}