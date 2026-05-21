using System;
using System.Collections.Generic;

namespace FitPanel.DTOs.Diet;

public class DietResponseDto
{
    public int Id { get; set; }
    public int NumberOfMeals { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<DietMealResponseDto> DietMeals { get; set; } = new();

    public DietResponseDto() { }
    public DietResponseDto(int id, int numberOfMeals, DateTime createdAt, List<DietMealResponseDto> dietMeals)
    {
        Id = id;
        NumberOfMeals = numberOfMeals;
        CreatedAt = createdAt;
        DietMeals = dietMeals;
    }
}

public class DietMealResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Instruction { get; set; }
    public string? Link { get; set; }  // Video/recipe link for the whole meal
    public List<MealItemResponseDto> Ingredients { get; set; } = new();
    public List<DietMealResponseDto> AlternativeMeals { get; set; } = new();

    public DietMealResponseDto() { }
    public DietMealResponseDto(int id, string name, string? instruction, string? link,
        List<MealItemResponseDto> ingredients, List<DietMealResponseDto> alternativeMeals)
    {
        Id = id;
        Name = name;
        Instruction = instruction;
        Link = link;
        Ingredients = ingredients;
        AlternativeMeals = alternativeMeals;
    }
}
