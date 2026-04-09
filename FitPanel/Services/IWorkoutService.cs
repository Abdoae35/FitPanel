// Services/IWorkoutService.cs
using FitPanel.DTOs.Workout;
using FitPanel.Data.Models;

namespace FitPanel.Services;

public interface IWorkoutService
{
    Task<WorkoutResponseDto> CreateWorkoutAsync(int clientId, CreateWorkoutDto dto, string coachId);
    Task<List<WorkoutResponseDto>> GetWorkoutsAsync(int clientId, string coachId);
    Task<WorkoutDayResponseDto?> AddWorkoutDayAsync(int clientId, int workoutId, CreateWorkoutDayDto dto, string coachId);
    Task<ExerciseResponseDto?> AddExerciseAsync(int clientId, int workoutId, int dayId, CreateExerciseDto dto, string coachId);
    Task<(bool Success, string Message)> DeleteWorkoutAsync(int clientId, int workoutId, string coachId);
   Task<(bool Success, string Message)> DeleteExerciseAsync(int exerciseId, string coachId);
    // Services/IWorkoutService.cs — add these two methods
Task<CardioResponseDto?> AddCardioAsync(int clientId, int workoutId, int dayId, CreateCardioDto dto, string coachId);
Task<(bool Success, string Message)> DeleteCardioAsync(int clientId, int workoutId, int dayId, string coachId);
}