// Services/IWorkoutService.cs
using FitPanel.DTOs.Workout;

namespace FitPanel.Services;

public interface IWorkoutService
{
    Task<WorkoutResponseDto> CreateWorkoutAsync(int clientId, CreateWorkoutDto dto, string coachId);
    Task<List<WorkoutResponseDto>> GetWorkoutsAsync(int clientId, string coachId);
    Task<(bool Success, string Message)> DeleteWorkoutAsync(int clientId, int workoutId, string coachId);
    Task<WorkoutDayResponseDto?> AddWorkoutDayAsync(int clientId, int workoutId, CreateWorkoutDayDto dto, string coachId);
    Task<ExerciseResponseDto?> AddExerciseAsync(int clientId, int workoutId, int dayId, CreateExerciseDto dto, string coachId);
    Task<CardioResponseDto?> AddCardioAsync(int clientId, int workoutId, int dayId, CreateCardioDto dto, string coachId);
    Task<(bool Success, string Message)> DeleteCardioAsync(int clientId, int workoutId, int dayId, string coachId);
    Task<(bool Success, string Message)> DeleteExerciseAsync(int exerciseId, string coachId);      // ← NEW
    Task<(bool Success, string Message)> UpdateExerciseAsync(int exerciseId, CreateExerciseDto dto, string coachId); // ← NEW
    Task<(bool Success, string Message)> UpdateCardioAsync(
    int clientId, int workoutId, int dayId, CreateCardioDto dto, string coachId);
    Task<WorkoutDayResponseDto?> UpdateWorkoutDayNotesAsync(int clientId, int workoutId, int dayId, string? notes, string coachId);
    Task<(bool Success, string Message)> DeleteWorkoutDayAsync(int clientId, int workoutId, int dayId, string coachId);
    Task<(bool Success, string Message)> UpdateWorkoutDayAsync(int clientId, int workoutId, int dayId, string dayName, FitPanel.Data.Enums.Days dayOfWeek, string coachId);
    Task<(bool Success, string Message)> UpdateWarmUpAsync(int clientId, int workoutId, int dayId, string? warmUpName, string? warmUpLink, string coachId);
}