// DTOs/Diet/CreateDietDto.cs
namespace FitPanel.DTOs.Diet;

public class CreateDietDto
{
    public int NumberOfMeals { get; set; } = 3;
}

public class CreateMealItemDto
{
    public string MealName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Protein { get; set; }
    public int Carbs { get; set; }
    public int Fats { get; set; }
    public int Calories { get; set; }
    public string? Link { get; set; }
}

public record MealItemResponseDto(
    int Id,
    string MealName,
    string Description,
    int Protein,
    int Carbs,
    int Fats,
    int Calories,
    string? Link
);

public class DietResponseDto
{
    public int Id { get; set; }
    public int NumberOfMeals { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<MealItemResponseDto> MealItems { get; set; } = new();

    public DietResponseDto() { }
    public DietResponseDto(int id, int numberOfMeals, DateTime createdAt, List<MealItemResponseDto> mealItems)
    {
        Id = id;
        NumberOfMeals = numberOfMeals;
        CreatedAt = createdAt;
        MealItems = mealItems;
    }
}
