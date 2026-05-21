// Services/AlternativeService.cs
using FitPanel.Data;
using FitPanel.Data.Models;
using FitPanel.DTOs.Diet;
using Microsoft.EntityFrameworkCore;

namespace FitPanel.Services;

public class AlternativeService : IAlternativeService
{
    private readonly FitPanelDbContext _db;
    private readonly INutritionApiService _nutritionApi;

    public AlternativeService(FitPanelDbContext db, INutritionApiService nutritionApi)
    {
        _db = db;
        _nutritionApi = nutritionApi;
    }

    public async Task<AlternativeResponeDto> AddAlternativeAsync(
        int mealItemId, CreateAlternativeDto dto, string coachId)
    {
        // Verify ownership: coachId → client → diet → mealitem
        var mealItem = await _db.MealItems
            .Include(m => m.DietMeal)
                .ThenInclude(dm => dm.Diet)
                    .ThenInclude(d => d.Client)
            .FirstOrDefaultAsync(m => m.Id == mealItemId
                && m.DietMeal.Diet.Client.CoachId == coachId)
            ?? throw new UnauthorizedAccessException("Meal item not found.");

        var apiData = await _nutritionApi.GetNutritionAsync($"{dto.Quantity} {dto.Unit} {dto.MealName}");

        var alternative = new AlternativeItem
        {
            MealItemId = mealItemId,
            MealName = dto.MealName,
            Description = dto.Description,
            Quantity = dto.Quantity,
            Unit = dto.Unit,
            Calories = apiData != null ? (int)apiData.Calories : dto.Calories,
            Protein = apiData != null ? (int)apiData.Protein : dto.Protein,
            Carbs = apiData != null ? (int)apiData.Carbs : dto.Carbs,
            Fats = apiData != null ? (int)apiData.Fats : dto.Fats,
        };

        _db.AlternativeItems.Add(alternative);
        await _db.SaveChangesAsync();

        return new AlternativeResponeDto(
            alternative.Id,
            alternative.MealName,
            alternative.Description,
            alternative.Quantity,
            alternative.Unit,
            alternative.Protein,
            alternative.Carbs,
            alternative.Fats,
            alternative.Calories);
    }

    public async Task<(bool Success, string Message)> UpdateAlternativeAsync(
        int alternativeId, CreateAlternativeDto dto, string coachId)
    {
        var alternative = await _db.AlternativeItems
            .Include(a => a.MealItem)
                .ThenInclude(m => m.DietMeal)
                    .ThenInclude(dm => dm.Diet)
                        .ThenInclude(d => d.Client)
            .FirstOrDefaultAsync(a => a.Id == alternativeId
                && a.MealItem.DietMeal.Diet.Client.CoachId == coachId);

        if (alternative == null)
            return (false, "Alternative not found.");

        var apiData = await _nutritionApi.GetNutritionAsync($"{dto.Quantity} {dto.Unit} {dto.MealName}");

        alternative.MealName = dto.MealName;
        alternative.Description = dto.Description;
        alternative.Quantity = dto.Quantity;
        alternative.Unit = dto.Unit;
        alternative.Calories = apiData != null ? (int)apiData.Calories : dto.Calories;
        alternative.Protein = apiData != null ? (int)apiData.Protein : dto.Protein;
        alternative.Carbs = apiData != null ? (int)apiData.Carbs : dto.Carbs;
        alternative.Fats = apiData != null ? (int)apiData.Fats : dto.Fats;

        await _db.SaveChangesAsync();
        return (true, "Alternative updated.");
    }

    public async Task<(bool Success, string Message)> DeleteAlternativeAsync(
        int alternativeId, string coachId)
    {
        var alternative = await _db.AlternativeItems
            .Include(a => a.MealItem)
                .ThenInclude(m => m.DietMeal)
                    .ThenInclude(dm => dm.Diet)
                        .ThenInclude(d => d.Client)
            .FirstOrDefaultAsync(a => a.Id == alternativeId
                && a.MealItem.DietMeal.Diet.Client.CoachId == coachId);

        if (alternative == null)
            return (false, "Alternative not found.");

        _db.AlternativeItems.Remove(alternative);
        await _db.SaveChangesAsync();
        return (true, "Alternative deleted.");
    }
}