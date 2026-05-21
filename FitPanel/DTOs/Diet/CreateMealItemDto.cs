namespace FitPanel.DTOs.Diet;

// Individual ingredient — no Link (link is on the DietMeal whole-meal level)
public class CreateMealItemDto
{
    public string MealName { get; set; } = string.Empty;
    public double Quantity { get; set; } = 1;
    public string? Unit { get; set; }
    public int Protein { get; set; }
    public int Carbs { get; set; }
    public int Fats { get; set; }
    public int Calories { get; set; }
}
