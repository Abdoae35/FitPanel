namespace FitPanel.DTOs.Diet;

// AlternativeItem is an ingredient-level swap — no Link
public record AlternativeResponeDto
(
    int Id,
    string MealName,
    string Description,
    double Quantity,
    string? Unit,
    int Protein,
    int Carbs,
    int Fats,
    int Calories
);
