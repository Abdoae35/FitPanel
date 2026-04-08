using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitPanel.DTOs.Diet;
  public record DietResponseDto(
    int Id,
    int NumberOfMeals,
    DateTime CreatedAt,
    List<MealItemResponseDto> MealItems
);
