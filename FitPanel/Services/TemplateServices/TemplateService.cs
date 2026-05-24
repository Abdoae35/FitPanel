using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FitPanel.Data;
using FitPanel.Data.Models;
using FitPanel.DTOs.Diet;
using FitPanel.DTOs.Workout;
using Microsoft.EntityFrameworkCore;

namespace FitPanel.Services;

public class TemplateService : ITemplateService
{
    private readonly FitPanelDbContext _db;

    public TemplateService(FitPanelDbContext db)
    {
        _db = db;
    }

    // ─── DIET TEMPLATES ──────────────────────────────────────────────────────

    public async Task<DietTemplate> SaveDietAsTemplateAsync(int dietId, string templateName, string coachId)
    {
        var diet = await _db.Diets
            .Include(d => d.DietMeals)
                .ThenInclude(dm => dm.AlternativeMeals)
                    .ThenInclude(am => am.MealItems)
                        .ThenInclude(m => m.AlternativeItems)
            .Include(d => d.DietMeals)
                .ThenInclude(dm => dm.MealItems)
                    .ThenInclude(m => m.AlternativeItems)
            .FirstOrDefaultAsync(d => d.Id == dietId && d.Client.CoachId == coachId)
            ?? throw new KeyNotFoundException("Diet plan not found.");

        // Map to a clean, serializable DTO list
        var mainMeals = diet.DietMeals
            .Where(dm => dm.ParentDietMealId == null)
            .Select(dm => MapDietMealToDto(dm))
            .ToList();

        var json = JsonSerializer.Serialize(mainMeals);

        var template = new DietTemplate
        {
            Name = templateName,
            CoachId = coachId,
            DietJson = json,
            Instructions = diet.Instructions,
            NumberOfMeals = diet.NumberOfMeals,
            CreatedAt = DateTime.UtcNow
        };

        _db.DietTemplates.Add(template);
        await _db.SaveChangesAsync();

        return template;
    }

    public async Task<bool> CloneDietTemplateAsync(int templateId, int clientId, string coachId)
    {
        var template = await _db.DietTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId && t.CoachId == coachId);

        if (template == null) return false;

        var client = await _db.Clients
            .FirstOrDefaultAsync(c => c.Id == clientId && c.CoachId == coachId);
        if (client == null) return false;

        // Create new Diet record
        var diet = new Diet
        {
            ClientId = clientId,
            NumberOfMeals = template.NumberOfMeals,
            Instructions = template.Instructions,
            CreatedAt = DateTime.UtcNow
        };
        _db.Diets.Add(diet);
        await _db.SaveChangesAsync();

        // Deserialize meals
        var mealDtos = JsonSerializer.Deserialize<List<DietMealResponseDto>>(template.DietJson);
        if (mealDtos != null)
        {
            foreach (var mealDto in mealDtos)
            {
                await CloneMealRecursiveAsync(mealDto, diet.Id, null);
            }
        }

        return true;
    }

    public async Task<List<DietTemplate>> GetDietTemplatesAsync(string coachId)
    {
        return await _db.DietTemplates
            .Where(t => t.CoachId == coachId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> DeleteDietTemplateAsync(int templateId, string coachId)
    {
        var template = await _db.DietTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId && t.CoachId == coachId);

        if (template == null) return false;

        _db.DietTemplates.Remove(template);
        await _db.SaveChangesAsync();
        return true;
    }

    // ─── WORKOUT TEMPLATES ───────────────────────────────────────────────────

    public async Task<WorkoutTemplate> SaveWorkoutAsTemplateAsync(int workoutId, string templateName, string coachId)
    {
        var workout = await _db.WorkOuts
            .Include(w => w.WorkOutDays)
                .ThenInclude(d => d.ExcerciseItems)
            .Include(w => w.WorkOutDays)
                .ThenInclude(d => d.Cardio)
            .FirstOrDefaultAsync(w => w.Id == workoutId && w.Client.CoachId == coachId)
            ?? throw new KeyNotFoundException("Workout plan not found.");

        var daysData = workout.WorkOutDays.Select(d => new WorkoutDayResponseDto(
            d.Id,
            d.DayName,
            d.Day,
            d.ExcerciseItems.Select(e => new ExerciseResponseDto(
                e.Id,
                e.ExerciseName,
                e.Sets,
                e.Reps,
                e.RestTime,
                e.ExcerciseLink
            )).ToList(),
            d.Cardio == null ? null : new CardioResponseDto(
                d.Cardio.Id,
                d.Cardio.CardioType,
                d.Cardio.DurationMinutes,
                d.Cardio.Intensity,
                d.Cardio.Notes)
        )).ToList();

        var json = JsonSerializer.Serialize(daysData);

        var template = new WorkoutTemplate
        {
            Name = templateName,
            CoachId = coachId,
            WorkoutJson = json,
            SplitName = workout.SplitName,
            Instructions = workout.Instructions,
            NumberOfWorkoutDays = workout.NumberOfWorkOutDays,
            CreatedAt = DateTime.UtcNow
        };

        _db.WorkoutTemplates.Add(template);
        await _db.SaveChangesAsync();

        return template;
    }

    public async Task<bool> CloneWorkoutTemplateAsync(int templateId, int clientId, string coachId)
    {
        var template = await _db.WorkoutTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId && t.CoachId == coachId);

        if (template == null) return false;

        var client = await _db.Clients
            .FirstOrDefaultAsync(c => c.Id == clientId && c.CoachId == coachId);
        if (client == null) return false;

        // Create a new workout
        var workout = new WorkOut
        {
            ClientId = clientId,
            SplitName = template.SplitName,
            Instructions = template.Instructions,
            NumberOfWorkOutDays = template.NumberOfWorkoutDays,
            CreatedAt = DateTime.UtcNow
        };
        _db.WorkOuts.Add(workout);
        await _db.SaveChangesAsync();

        var daysDtos = JsonSerializer.Deserialize<List<WorkoutDayResponseDto>>(template.WorkoutJson);
        if (daysDtos != null)
        {
            foreach (var dayDto in daysDtos)
            {
                var day = new WorkOutDay
                {
                    WorkOutId = workout.Id,
                    DayName = dayDto.DayName,
                    Day = dayDto.Day
                };
                _db.WorkOutDays.Add(day);
                await _db.SaveChangesAsync(); // get Day ID

                // Exercises
                foreach (var exDto in dayDto.Exercises)
                {
                    var ex = new Excercise
                    {
                        WorkOutDayId = day.Id,
                        ExerciseName = exDto.ExerciseName,
                        Sets = exDto.Sets,
                        Reps = exDto.Reps,
                        RestTime = exDto.RestTime,
                        ExcerciseLink = exDto.ExerciseLink
                    };
                    _db.Excercises.Add(ex);
                }

                // Cardio
                if (dayDto.Cardio != null)
                {
                    var cardio = new Cardio
                    {
                        WorkOutDayId = day.Id,
                        CardioType = dayDto.Cardio.CardioType,
                        DurationMinutes = dayDto.Cardio.DurationMinutes,
                        Intensity = dayDto.Cardio.Intensity,
                        Notes = dayDto.Cardio.Notes
                    };
                    _db.Cardios.Add(cardio);
                }
            }
            await _db.SaveChangesAsync();
        }

        return true;
    }

    public async Task<List<WorkoutTemplate>> GetWorkoutTemplatesAsync(string coachId)
    {
        return await _db.WorkoutTemplates
            .Where(t => t.CoachId == coachId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> DeleteWorkoutTemplateAsync(int templateId, string coachId)
    {
        var template = await _db.WorkoutTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId && t.CoachId == coachId);

        if (template == null) return false;

        _db.WorkoutTemplates.Remove(template);
        await _db.SaveChangesAsync();
        return true;
    }

    // ─── PRIVATE HELPERS ─────────────────────────────────────────────────────

    private DietMealResponseDto MapDietMealToDto(DietMeal dm)
    {
        return new DietMealResponseDto(
            dm.Id,
            dm.Name,
            dm.Instruction,
            dm.Link,
            dm.MealItems.Select(mi => new MealItemResponseDto(
                mi.Id,
                mi.MealName,
                mi.Quantity,
                mi.Unit,
                mi.Protein,
                mi.Carbs,
                mi.Fats,
                mi.Calories,
                mi.AlternativeItems.Select(ai => new AlternativeResponeDto(
                    ai.Id,
                    ai.MealName,
                    ai.Description,
                    ai.Quantity,
                    ai.Unit,
                    ai.Protein,
                    ai.Carbs,
                    ai.Fats,
                    ai.Calories
                )).ToList()
            )).ToList(),
            dm.AlternativeMeals.Select(am => MapDietMealToDto(am)).ToList()
        );
    }

    private async Task CloneMealRecursiveAsync(DietMealResponseDto mealDto, int dietId, int? parentId)
    {
        var dm = new DietMeal
        {
            DietId = dietId,
            Name = mealDto.Name,
            Instruction = mealDto.Instruction,
            Link = mealDto.Link,
            ParentDietMealId = parentId
        };
        _db.DietMeals.Add(dm);
        await _db.SaveChangesAsync(); // gets ID for dm

        // Add ingredients
        foreach (var ingDto in mealDto.Ingredients)
        {
            var mi = new MealItem
            {
                DietMealId = dm.Id,
                MealName = ingDto.MealName,
                Quantity = ingDto.Quantity,
                Unit = ingDto.Unit,
                Protein = ingDto.Protein,
                Carbs = ingDto.Carbs,
                Fats = ingDto.Fats,
                Calories = ingDto.Calories
            };
            _db.MealItems.Add(mi);
            await _db.SaveChangesAsync(); // gets ID for mi

            // Add ingredient alternatives
            if (ingDto.Alternatives != null)
            {
                foreach (var altDto in ingDto.Alternatives)
                {
                    var ai = new AlternativeItem
                    {
                        MealItemId = mi.Id,
                        MealName = altDto.MealName,
                        Description = altDto.Description,
                        Protein = altDto.Protein,
                        Carbs = altDto.Carbs,
                        Fats = altDto.Fats,
                        Calories = altDto.Calories,
                        Quantity = altDto.Quantity,
                        Unit = altDto.Unit
                    };
                    _db.AlternativeItems.Add(ai);
                }
            }
        }
        await _db.SaveChangesAsync();

        // Recursively clone alternative meals
        if (mealDto.AlternativeMeals != null)
        {
            foreach (var altMealDto in mealDto.AlternativeMeals)
            {
                await CloneMealRecursiveAsync(altMealDto, dietId, dm.Id);
            }
        }
    }
}
