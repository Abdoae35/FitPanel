using System;
using System.Collections.Generic;

namespace FitPanel.Data.Models;

public class DietMeal
{
    public int Id { get; set; }
    public string Name { get; set; } // e.g., "Breakfast"
    public string? Instruction { get; set; }
    public string? Link { get; set; } // Video/recipe link for the whole meal
    public int DietId { get; set; }
    public Diet Diet { get; set; }

    // Nullable Parent ID for alternative whole meals
    public int? ParentDietMealId { get; set; }
    public DietMeal? ParentDietMeal { get; set; }
    public ICollection<DietMeal> AlternativeMeals { get; set; } = new List<DietMeal>();

    public ICollection<MealItem> MealItems { get; set; } = new List<MealItem>();
}
