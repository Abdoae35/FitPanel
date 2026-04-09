using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitPanel.DTOs.Diet;
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
