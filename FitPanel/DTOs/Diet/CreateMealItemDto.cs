
namespace FitPanel.DTOs.Diet;

  public record CreateMealItemDto(
    string MealName,
    string Description,
    int Protein,
    int Carbs,
    int Fats,
    int Calories,
    string? Link
);


