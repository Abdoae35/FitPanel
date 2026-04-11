using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitPanel.DTOs.Diet;
    public record AlternativeResponeDto
    (
        int Id,
        string MealName,
        string Description,
        int Protein,
        int Carbs,
        int Fats,
        int Calories,
        string? Link
    );
