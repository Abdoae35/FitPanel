using System.Collections.Generic;
using System.Threading.Tasks;
using FitPanel.Data.Models;

namespace FitPanel.Services;

public interface ITemplateService
{
    // Diet Templates
    Task<DietTemplate> SaveDietAsTemplateAsync(int dietId, string templateName, string coachId);
    Task<bool> CloneDietTemplateAsync(int templateId, int clientId, string coachId);
    Task<List<DietTemplate>> GetDietTemplatesAsync(string coachId);
    Task<bool> DeleteDietTemplateAsync(int templateId, string coachId);

    // Workout Templates
    Task<WorkoutTemplate> SaveWorkoutAsTemplateAsync(int workoutId, string templateName, string coachId);
    Task<bool> CloneWorkoutTemplateAsync(int templateId, int clientId, string coachId);
    Task<List<WorkoutTemplate>> GetWorkoutTemplatesAsync(string coachId);
    Task<bool> DeleteWorkoutTemplateAsync(int templateId, string coachId);
}
