using FitPanel.Data;
using FitPanel.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FitPanel.Services;

public class CoachDictionaryService : ICoachDictionaryService
{
    private readonly FitPanelDbContext _db;

    public CoachDictionaryService(FitPanelDbContext db)
    {
        _db = db;
    }

    public async Task<List<CoachMealDictionary>> GetMealsAsync(string coachId)
    {
        return await _db.CoachMealDictionaries
            .Where(x => x.CoachId == coachId)
            .OrderBy(x => x.MealName)
            .ToListAsync();
    }

    public async Task<CoachMealDictionary> AddMealAsync(string coachId, CoachMealDictionary meal)
    {
        meal.CoachId = coachId;
        _db.CoachMealDictionaries.Add(meal);
        await _db.SaveChangesAsync();
        return meal;
    }

    public async Task<(bool Success, string Message)> UpdateMealAsync(string coachId, CoachMealDictionary meal)
    {
        var existing = await _db.CoachMealDictionaries.FirstOrDefaultAsync(x => x.Id == meal.Id && x.CoachId == coachId);
        if (existing == null) return (false, "Meal not found.");

        existing.MealName = meal.MealName;
        existing.Protein = meal.Protein;
        existing.Carbs = meal.Carbs;
        existing.Fats = meal.Fats;
        existing.Calories = meal.Calories;
        existing.Link = meal.Link;
        existing.Instruction = meal.Instruction;
        existing.IngredientsJson = meal.IngredientsJson;

        await _db.SaveChangesAsync();
        return (true, "Meal updated.");
    }

    public async Task<(bool Success, string Message)> DeleteMealAsync(string coachId, int id)
    {
        var existing = await _db.CoachMealDictionaries.FirstOrDefaultAsync(x => x.Id == id && x.CoachId == coachId);
        if (existing == null) return (false, "Meal not found.");

        _db.CoachMealDictionaries.Remove(existing);
        await _db.SaveChangesAsync();
        return (true, "Meal deleted.");
    }

    public async Task<List<CoachExerciseDictionary>> GetExercisesAsync(string coachId)
    {
        return await _db.CoachExerciseDictionaries
            .Where(x => x.CoachId == coachId)
            .OrderBy(x => x.ExerciseName)
            .ToListAsync();
    }

    public async Task<CoachExerciseDictionary> AddExerciseAsync(string coachId, CoachExerciseDictionary exercise)
    {
        exercise.CoachId = coachId;
        _db.CoachExerciseDictionaries.Add(exercise);
        await _db.SaveChangesAsync();
        return exercise;
    }

    public async Task<(bool Success, string Message)> UpdateExerciseAsync(string coachId, CoachExerciseDictionary exercise)
    {
        var existing = await _db.CoachExerciseDictionaries.FirstOrDefaultAsync(x => x.Id == exercise.Id && x.CoachId == coachId);
        if (existing == null) return (false, "Exercise not found.");

        existing.ExerciseName = exercise.ExerciseName;
        existing.ExcerciseLink = exercise.ExcerciseLink;

        await _db.SaveChangesAsync();
        return (true, "Exercise updated.");
    }

    public async Task<(bool Success, string Message)> DeleteExerciseAsync(string coachId, int id)
    {
        var existing = await _db.CoachExerciseDictionaries.FirstOrDefaultAsync(x => x.Id == id && x.CoachId == coachId);
        if (existing == null) return (false, "Exercise not found.");

        _db.CoachExerciseDictionaries.Remove(existing);
        await _db.SaveChangesAsync();
        return (true, "Exercise deleted.");
    }
}
