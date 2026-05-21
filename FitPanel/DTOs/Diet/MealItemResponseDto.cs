using System.Collections.Generic;

namespace FitPanel.DTOs.Diet;

// MealItem (ingredient) no longer has a Link — links live on the DietMeal (whole meal) level
public record MealItemResponseDto(
    int Id,
    string MealName,
    double Quantity,
    string? Unit,
    int Protein,
    int Carbs,
    int Fats,
    int Calories,
    List<AlternativeResponeDto> Alternatives
);