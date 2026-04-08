// Services/DietService.cs
using FitPanel.Data;
using FitPanel.Data.Models;
using FitPanel.DTOs.Diet;
using Microsoft.EntityFrameworkCore;

namespace FitPanel.Services;

public class DietService : IDietService
{
    private readonly FitPanelDbContext _db;

    public DietService(FitPanelDbContext db)
    {
        _db = db;
    }

    public async Task<DietResponseDto> CreateDietAsync(
        int clientId, CreateDietDto dto, string coachId)
    {
        // Verify client belongs to this coach
        var client = await _db.Clients
            .FirstOrDefaultAsync(c => c.Id == clientId && c.CoachId == coachId)
            ?? throw new UnauthorizedAccessException("Client not found.");

        var diet = new Diet
        {
            ClientId = clientId,
            NumberOfMeals = dto.NumberOfMeals,
            CreatedAt = DateTime.UtcNow
        };

        _db.Diets.Add(diet);
        await _db.SaveChangesAsync();

        return MapDietToDto(diet, new List<MealItem>());
    }

    public async Task<List<DietResponseDto>> GetDietsAsync(int clientId, string coachId)
    {
        var client = await _db.Clients
            .FirstOrDefaultAsync(c => c.Id == clientId && c.CoachId == coachId)
            ?? throw new UnauthorizedAccessException("Client not found.");

        return await _db.Diets
            .Where(d => d.ClientId == clientId)
            .Include(d => d.MealItems)
            .Select(d => new DietResponseDto(
                d.Id,
                d.NumberOfMeals,
                d.CreatedAt,
                d.MealItems.Select(m => new MealItemResponseDto(
                    m.Id, m.MealName, m.Description,
                    m.Protein, m.Carbs, m.Fats, m.Calories, m.Link
                )).ToList()
            ))
            .ToListAsync();
    }

    public async Task<DietResponseDto?> AddMealItemAsync(
        int clientId, int dietId, CreateMealItemDto dto, string coachId)
    {
        var diet = await _db.Diets
            .Include(d => d.MealItems)
            .Include(d => d.Client)
            .FirstOrDefaultAsync(d => d.Id == dietId
                && d.ClientId == clientId
                && d.Client.CoachId == coachId);

        if (diet == null) return null;

        var meal = new MealItem
        {
            DietId = dietId,
            MealName = dto.MealName,
            Description = dto.Description,
            Protein = dto.Protein,
            Carbs = dto.Carbs,
            Fats = dto.Fats,
            Calories = dto.Calories,
            Link = dto.Link
        };

        _db.MealItems.Add(meal);
        await _db.SaveChangesAsync();

        return MapDietToDto(diet, diet.MealItems.ToList());
    }

    public async Task<(bool Success, string Message)> DeleteDietAsync(
        int clientId, int dietId, string coachId)
    {
        var diet = await _db.Diets
            .Include(d => d.Client)
            .FirstOrDefaultAsync(d => d.Id == dietId
                && d.ClientId == clientId
                && d.Client.CoachId == coachId);

        if (diet == null) return (false, "Diet not found.");

        _db.Diets.Remove(diet);
        await _db.SaveChangesAsync();
        return (true, "Diet deleted.");
    }

    private DietResponseDto MapDietToDto(Diet diet, List<MealItem> meals) =>
        new(diet.Id, diet.NumberOfMeals, diet.CreatedAt,
            meals.Select(m => new MealItemResponseDto(
                m.Id, m.MealName, m.Description,
                m.Protein, m.Carbs, m.Fats, m.Calories, m.Link
            )).ToList());
}