// Services/AlternativeService.cs
using FitPanel.Data;
using FitPanel.Data.Models;
using FitPanel.DTOs.Diet;
using Microsoft.EntityFrameworkCore;

namespace FitPanel.Services;

public class AlternativeService : IAlternativeService
{
    private readonly FitPanelDbContext _db;

    public AlternativeService(FitPanelDbContext db)
    {
        _db = db;
    }

    public async Task<AlternativeResponeDto> AddAlternativeAsync(
        int mealItemId, CreateAlternativeDto dto, string coachId)
    {
        // Verify ownership: coachId → client → diet → mealitem
        var mealItem = await _db.MealItems
            .Include(m => m.Diet)
                .ThenInclude(d => d.Client)
            .FirstOrDefaultAsync(m => m.Id == mealItemId
                && m.Diet.Client.CoachId == coachId)
            ?? throw new UnauthorizedAccessException("Meal item not found.");

        var alternative = new AlternativeItem
        {
            MealItemId = mealItemId,
            MealName = dto.MealName,
            Description = dto.Description,
            Protein = dto.Protein,
            Carbs = dto.Carbs,
            Fats = dto.Fats,
            Calories = dto.Calories,
            Link = dto.Link
        };

        _db.AlternativeItems.Add(alternative);
        await _db.SaveChangesAsync();

        return new AlternativeResponeDto(
            alternative.Id,
            alternative.MealName,
            alternative.Description,
            alternative.Protein,
            alternative.Carbs,
            alternative.Fats,
            alternative.Calories,
            alternative.Link);
    }

    public async Task<(bool Success, string Message)> UpdateAlternativeAsync(
        int alternativeId, CreateAlternativeDto dto, string coachId)
    {
        var alternative = await _db.AlternativeItems
            .Include(a => a.MealItem)
                .ThenInclude(m => m.Diet)
                    .ThenInclude(d => d.Client)
            .FirstOrDefaultAsync(a => a.Id == alternativeId
                && a.MealItem.Diet.Client.CoachId == coachId);

        if (alternative == null)
            return (false, "Alternative not found.");

        alternative.MealName = dto.MealName;
        alternative.Description = dto.Description;
        alternative.Protein = dto.Protein;
        alternative.Carbs = dto.Carbs;
        alternative.Fats = dto.Fats;
        alternative.Calories = dto.Calories;
        if (dto.Link != null) alternative.Link = dto.Link;

        await _db.SaveChangesAsync();
        return (true, "Alternative updated.");
    }

    public async Task<(bool Success, string Message)> DeleteAlternativeAsync(
        int alternativeId, string coachId)
    {
        var alternative = await _db.AlternativeItems
            .Include(a => a.MealItem)
                .ThenInclude(m => m.Diet)
                    .ThenInclude(d => d.Client)
            .FirstOrDefaultAsync(a => a.Id == alternativeId
                && a.MealItem.Diet.Client.CoachId == coachId);

        if (alternative == null)
            return (false, "Alternative not found.");

        _db.AlternativeItems.Remove(alternative);
        await _db.SaveChangesAsync();
        return (true, "Alternative deleted.");
    }
}