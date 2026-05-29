// Services/DietService.cs
using FitPanel.Data;
using FitPanel.Data.Models;
using FitPanel.DTOs.Diet;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FitPanel.Services;

public class DietService : IDietService
{
    private readonly FitPanelDbContext _db;
    private readonly INutritionApiService _nutritionApi;

    public DietService(FitPanelDbContext db, INutritionApiService nutritionApi)
    {
        _db = db;
        _nutritionApi = nutritionApi;
    }

    // ── Create Diet ─────────────────────────────────────────────────────────

    public async Task<DietResponseDto> CreateDietAsync(
        int clientId, CreateDietDto dto, string coachId)
    {
        var client = await _db.Clients
            .FirstOrDefaultAsync(c => c.Id == clientId && c.CoachId == coachId)
            ?? throw new UnauthorizedAccessException("Client not found.");

        var diet = new Diet
        {
            ClientId = clientId,
            NumberOfMeals = dto.NumberOfMeals,
            Instructions = dto.Instructions,
            CreatedAt = DateTime.UtcNow
        };

        _db.Diets.Add(diet);
        await _db.SaveChangesAsync();

        return MapDietToDto(diet, new List<DietMeal>());
    }

    // ── Get Diets ────────────────────────────────────────────────────────────

    public async Task<List<DietResponseDto>> GetDietsAsync(int clientId, string coachId)
    {
        var client = await _db.Clients
            .FirstOrDefaultAsync(c => c.Id == clientId && c.CoachId == coachId)
            ?? throw new UnauthorizedAccessException("Client not found.");

        var diets = await _db.Diets
            .Where(d => d.ClientId == clientId)
            .Include(d => d.DietMeals)
                .ThenInclude(dm => dm.AlternativeMeals)
                    .ThenInclude(am => am.MealItems)
                        .ThenInclude(m => m.AlternativeItems)
            .Include(d => d.DietMeals)
                .ThenInclude(dm => dm.MealItems)
                    .ThenInclude(m => m.AlternativeItems)
            .ToListAsync();

        return diets.Select(d => MapDietToDto(d, d.DietMeals.ToList())).ToList();
    }

    // ── Add DietMeal ─────────────────────────────────────────────────────────
    // Creates a named meal container (e.g. "Breakfast") with Link and optional InitialItems
    // from dictionary auto-fill. Also upserts the CoachMealDictionary.

    public async Task<DietResponseDto?> AddDietMealAsync(
        int clientId, int dietId, string mealName, string? instruction,
        int? parentDietMealId, string coachId, string? link = null,
        List<CreateMealItemDto>? initialItems = null)
    {
        var diet = await _db.Diets
            .Include(d => d.Client)
            .FirstOrDefaultAsync(d => d.Id == dietId && d.Client.CoachId == coachId);

        if (diet == null) return null;

        // Create the DietMeal container
        var dietMeal = new DietMeal
        {
            DietId = dietId,
            Name = mealName,
            Instruction = instruction,
            Link = link,
            ParentDietMealId = parentDietMealId
        };
        _db.DietMeals.Add(dietMeal);
        await _db.SaveChangesAsync();

        // If initial items provided (from dictionary auto-fill), create them all at once
        if (initialItems != null && initialItems.Count > 0)
        {
            foreach (var itemDto in initialItems)
            {
                var apiData = await _nutritionApi.GetNutritionAsync(
                    $"{itemDto.Quantity} {itemDto.Unit} {itemDto.MealName}");

                _db.MealItems.Add(new MealItem
                {
                    DietMealId = dietMeal.Id,
                    MealName = itemDto.MealName,
                    Quantity = itemDto.Quantity,
                    Unit = itemDto.Unit,
                    Calories = apiData != null ? (int)apiData.Calories : itemDto.Calories,
                    Protein = apiData != null ? (int)apiData.Protein : itemDto.Protein,
                    Carbs = apiData != null ? (int)apiData.Carbs : itemDto.Carbs,
                    Fats = apiData != null ? (int)apiData.Fats : itemDto.Fats,
                    AlternativeItems = new List<AlternativeItem>()
                });
            }
            await _db.SaveChangesAsync();
        }

        // Only upsert the dictionary when we actually have ingredients to store.
        // If this is a brand-new empty meal (no initialItems), skip it — the upsert
        // will happen when the first individual ingredient is added via AddMealItemAsync.
        // This prevents creating an empty-ingredients dictionary entry that would then
        // block future updates via the early-return guard in UpsertMealDictionaryAsync.
        if (initialItems != null && initialItems.Count > 0)
            await UpsertMealDictionaryAsync(dietMeal.Id, coachId);

        // Return full updated diet
        var fullDiet = await LoadFullDietAsync(dietId);
        return fullDiet != null ? MapDietToDto(fullDiet, fullDiet.DietMeals.ToList()) : null;
    }

    public async Task<DietResponseDto?> UpdateDietMealAsync(
        int clientId, int dietMealId, string mealName, string? instruction,
        string? link, string coachId)
    {
        var dietMeal = await _db.DietMeals
            .Include(dm => dm.Diet)
                .ThenInclude(d => d.Client)
            .FirstOrDefaultAsync(dm => dm.Id == dietMealId && dm.Diet.Client.CoachId == coachId);

        if (dietMeal == null) return null;

        dietMeal.Name = mealName;
        dietMeal.Instruction = instruction;
        dietMeal.Link = link;

        await _db.SaveChangesAsync();

        var fullDiet = await LoadFullDietAsync(dietMeal.DietId);
        return fullDiet != null ? MapDietToDto(fullDiet, fullDiet.DietMeals.ToList()) : null;
    }

    public async Task<(bool Success, string Message, DietResponseDto? UpdatedDiet)> DeleteDietMealAsync(
        int clientId, int dietMealId, string coachId)
    {
        var dietMeal = await _db.DietMeals
            .Include(dm => dm.Diet)
                .ThenInclude(d => d.Client)
            .FirstOrDefaultAsync(dm => dm.Id == dietMealId && dm.Diet.Client.CoachId == coachId);

        if (dietMeal == null) return (false, "Meal not found.", null);

        var dietId = dietMeal.DietId;

        // Cascade delete will automatically remove the children elements from DB
        _db.DietMeals.Remove(dietMeal);
        await _db.SaveChangesAsync();

        // NOTE: We DO NOT call UpsertMealDictionaryAsync here, preserving the template in the Dictionary!

        var fullDiet = await LoadFullDietAsync(dietId);
        var dto = fullDiet != null ? MapDietToDto(fullDiet, fullDiet.DietMeals.ToList()) : null;
        return (true, "Meal deleted.", dto);
    }

    // ── Add MealItem ─────────────────────────────────────────────────────────

    public async Task<DietResponseDto?> AddMealItemAsync(
        int clientId, int dietMealId, CreateMealItemDto dto, string coachId)
    {
        var dietMeal = await _db.DietMeals
            .Include(m => m.Diet)
                .ThenInclude(d => d.Client)
            .FirstOrDefaultAsync(m => m.Id == dietMealId && m.Diet.Client.CoachId == coachId);

        if (dietMeal == null) return null;

        var apiData = await _nutritionApi.GetNutritionAsync(
            $"{dto.Quantity} {dto.Unit} {dto.MealName}");

        var meal = new MealItem
        {
            DietMealId = dietMealId,
            MealName = dto.MealName,
            Quantity = dto.Quantity,
            Unit = dto.Unit,
            Calories = apiData != null ? (int)apiData.Calories : dto.Calories,
            Protein = apiData != null ? (int)apiData.Protein : dto.Protein,
            Carbs = apiData != null ? (int)apiData.Carbs : dto.Carbs,
            Fats = apiData != null ? (int)apiData.Fats : dto.Fats,
            AlternativeItems = new List<AlternativeItem>()
        };

        _db.MealItems.Add(meal);
        await _db.SaveChangesAsync();

        // Upsert dictionary — save full DietMeal as a template (unique by CoachId + DietMealName)
        await UpsertMealDictionaryAsync(dietMealId, coachId);

        var fullDiet = await LoadFullDietAsync(dietMeal.DietId);
        return fullDiet != null ? MapDietToDto(fullDiet, fullDiet.DietMeals.ToList()) : null;
    }

    // ── Delete Diet ──────────────────────────────────────────────────────────

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

    // ── Delete MealItem ──────────────────────────────────────────────────────

    public async Task<(bool Success, string Message)> DeleteMealItemAsync(int mealId, string coachId)
    {
        var meal = await _db.MealItems
            .Include(m => m.DietMeal)
                .ThenInclude(dm => dm.Diet)
                    .ThenInclude(d => d.Client)
            .FirstOrDefaultAsync(m => m.Id == mealId
                && m.DietMeal.Diet.Client.CoachId == coachId);

        if (meal == null) return (false, "Ingredient not found.");

        var dietMealId = meal.DietMealId;
        _db.MealItems.Remove(meal);
        await _db.SaveChangesAsync();

        return (true, "Ingredient deleted.");
    }

    // ── Update MealItem ──────────────────────────────────────────────────────

    public async Task<(bool Success, string Message)> UpdateMealItemAsync(
        int mealId, CreateMealItemDto dto, string coachId)
    {
        var meal = await _db.MealItems
            .Include(m => m.DietMeal)
                .ThenInclude(dm => dm.Diet)
                    .ThenInclude(d => d.Client)
            .FirstOrDefaultAsync(m => m.Id == mealId
                && m.DietMeal.Diet.Client.CoachId == coachId);

        if (meal == null) return (false, "Ingredient not found.");

        meal.MealName = dto.MealName;
        meal.Quantity = dto.Quantity;
        meal.Unit = dto.Unit;
        meal.Protein = dto.Protein;
        meal.Carbs = dto.Carbs;
        meal.Fats = dto.Fats;
        meal.Calories = dto.Calories;

        await _db.SaveChangesAsync();

        return (true, "Ingredient updated.");
    }

    // ── Dictionary Upsert ────────────────────────────────────────────────────
    // Called after every add/update/delete on a DietMeal's ingredients.
    // Saves the full DietMeal as a unique template in the CoachMealDictionary.
    // Unique key: (CoachId, MealName) — case-insensitive.

    private async Task UpsertMealDictionaryAsync(int dietMealId, string coachId)
    {
        // Load the DietMeal with all its current items
        var dietMeal = await _db.DietMeals
            .Include(dm => dm.MealItems)
            .FirstOrDefaultAsync(dm => dm.Id == dietMealId);

        if (dietMeal == null) return;

        // Skip alternative whole-meals (ParentDietMealId != null) from the dictionary
        // — we only save root meals as templates
        if (dietMeal.ParentDietMealId != null) return;

        var items = dietMeal.MealItems.ToList();

        // Serialize current ingredients snapshot
        var ingredientsJson = JsonSerializer.Serialize(
            items.Select(m => new
            {
                m.MealName,
                m.Quantity,
                m.Unit,
                m.Protein,
                m.Carbs,
                m.Fats,
                m.Calories
            }));

        var totalProtein  = items.Sum(m => m.Protein);
        var totalCarbs    = items.Sum(m => m.Carbs);
        var totalFats     = items.Sum(m => m.Fats);
        var totalCalories = items.Sum(m => m.Calories);

        // Find existing entry — unique by (CoachId, MealName) case-insensitive
        var existing = await _db.CoachMealDictionaries
            .FirstOrDefaultAsync(x =>
                x.CoachId == coachId &&
                x.MealName.ToLower() == dietMeal.Name.ToLower());

        if (existing != null)
        {
            // Always keep the dictionary entry up-to-date with the latest ingredients snapshot.
            // This ensures that when a coach adds ingredients to a new meal, those ingredients
            // are reflected in the dictionary so they auto-fill correctly for other clients.
            existing.IngredientsJson = ingredientsJson;
            existing.Protein    = totalProtein;
            existing.Carbs      = totalCarbs;
            existing.Fats       = totalFats;
            existing.Calories   = totalCalories;
            existing.Link       = dietMeal.Link;
            existing.Instruction = dietMeal.Instruction;
        }
        else
        {
            // Insert new unique template
            _db.CoachMealDictionaries.Add(new CoachMealDictionary
            {
                CoachId = coachId,
                MealName = dietMeal.Name,
                Link = dietMeal.Link,
                Instruction = dietMeal.Instruction,
                IngredientsJson = ingredientsJson,
                Protein = totalProtein,
                Carbs = totalCarbs,
                Fats = totalFats,
                Calories = totalCalories
            });
        }

        await _db.SaveChangesAsync();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<Diet?> LoadFullDietAsync(int dietId) =>
        await _db.Diets
            .Include(d => d.DietMeals)
                .ThenInclude(dm => dm.AlternativeMeals)
                    .ThenInclude(am => am.MealItems)
                        .ThenInclude(m => m.AlternativeItems)
            .Include(d => d.DietMeals)
                .ThenInclude(dm => dm.MealItems)
                    .ThenInclude(m => m.AlternativeItems)
            .FirstOrDefaultAsync(d => d.Id == dietId);

    // ── Update Diet Instructions ──────────────────────────────────────────────

    public async Task<DietResponseDto?> UpdateDietInstructionsAsync(
        int clientId, int dietId, string? instructions, string coachId)
    {
        var diet = await _db.Diets
            .Include(d => d.Client)
            .Include(d => d.DietMeals)
                .ThenInclude(dm => dm.AlternativeMeals)
                    .ThenInclude(am => am.MealItems)
                        .ThenInclude(m => m.AlternativeItems)
            .Include(d => d.DietMeals)
                .ThenInclude(dm => dm.MealItems)
                    .ThenInclude(m => m.AlternativeItems)
            .FirstOrDefaultAsync(d => d.Id == dietId && d.ClientId == clientId && d.Client.CoachId == coachId);

        if (diet == null) return null;

        diet.Instructions = instructions;
        await _db.SaveChangesAsync();

        return MapDietToDto(diet, diet.DietMeals.ToList());
    }

    // ── Private Mapper ───────────────────────────────────────────────────────

    private DietResponseDto MapDietToDto(Diet diet, List<DietMeal> dietMeals) =>
        new(
            diet.Id,
            diet.NumberOfMeals,
            diet.CreatedAt,
            diet.Instructions,
            dietMeals
            .Where(dm => dm.ParentDietMealId == null) // Only root meals at top level
            .OrderBy(dm => dm.Id)
            .Select(dm => new DietMealResponseDto(
                dm.Id,
                dm.Name,
                dm.Instruction,
                dm.Link,
                dm.MealItems.OrderBy(m => m.Id).Select(m => new MealItemResponseDto(
                    m.Id,
                    m.MealName,
                    m.Quantity,
                    m.Unit,
                    m.Protein,
                    m.Carbs,
                    m.Fats,
                    m.Calories,
                    m.AlternativeItems.OrderBy(a => a.Id).Select(a => new AlternativeResponeDto(
                        a.Id, a.MealName, a.Description, a.Quantity, a.Unit,
                        a.Protein, a.Carbs, a.Fats, a.Calories
                    )).ToList()
                )).ToList(),
                dm.AlternativeMeals?.OrderBy(a => a.Id).Select(a => new DietMealResponseDto(
                    a.Id,
                    a.Name,
                    a.Instruction,
                    a.Link,
                    a.MealItems.OrderBy(m => m.Id).Select(m => new MealItemResponseDto(
                        m.Id, m.MealName, m.Quantity, m.Unit,
                        m.Protein, m.Carbs, m.Fats, m.Calories,
                        m.AlternativeItems.OrderBy(alt => alt.Id).Select(alt => new AlternativeResponeDto(
                            alt.Id, alt.MealName, alt.Description, alt.Quantity, alt.Unit,
                            alt.Protein, alt.Carbs, alt.Fats, alt.Calories
                        )).ToList()
                    )).ToList(),
                    new List<DietMealResponseDto>() // Prevent infinite recursion
                )).ToList() ?? new List<DietMealResponseDto>()
            )).ToList()
        );
}