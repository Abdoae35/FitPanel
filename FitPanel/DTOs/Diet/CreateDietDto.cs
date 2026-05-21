// DTOs/Diet/CreateDietDto.cs
namespace FitPanel.DTOs.Diet;

public class CreateDietDto
{
    public int NumberOfMeals { get; set; } = 3;
    public string? Instructions { get; set; }
}

// Creates a named meal container (e.g. "Breakfast") with link + optional pre-populated ingredients
public class CreateDietMealDto
{
    public string Name { get; set; } = string.Empty;
    public string? Instruction { get; set; }
    public string? Link { get; set; } // Video/recipe link for the whole meal
    public int? ParentDietMealId { get; set; } // null = root meal, set = alternative whole-meal

    // Optional: pre-populate ingredients from dictionary auto-fill
    public List<CreateMealItemDto>? InitialItems { get; set; }
}
