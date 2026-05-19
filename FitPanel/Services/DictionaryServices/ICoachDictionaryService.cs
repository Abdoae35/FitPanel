using FitPanel.Data.Models;

namespace FitPanel.Services;

public interface ICoachDictionaryService
{
    Task<List<CoachMealDictionary>> GetMealsAsync(string coachId);
    Task<CoachMealDictionary> AddMealAsync(string coachId, CoachMealDictionary meal);
    Task<(bool Success, string Message)> UpdateMealAsync(string coachId, CoachMealDictionary meal);
    Task<(bool Success, string Message)> DeleteMealAsync(string coachId, int id);

    Task<List<CoachExerciseDictionary>> GetExercisesAsync(string coachId);
    Task<CoachExerciseDictionary> AddExerciseAsync(string coachId, CoachExerciseDictionary exercise);
    Task<(bool Success, string Message)> UpdateExerciseAsync(string coachId, CoachExerciseDictionary exercise);
    Task<(bool Success, string Message)> DeleteExerciseAsync(string coachId, int id);
}
