using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitPanel.DTOs.Diet;



 // DTOs/Diet/CreateDietDto.cs — update this record
public record MealItemResponseDto(
    int Id,
    string MealName,
    string Description,
    int Protein,
    int Carbs,
    int Fats,
    int Calories,
    string? Link,
    List<AlternativeResponeDto> Alternatives  // ← add this
);