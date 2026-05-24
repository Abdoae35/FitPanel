// Services/IDietService.cs
using FitPanel.DTOs.Diet;

namespace FitPanel.Services;

public interface IDietService
{
    Task<DietResponseDto> CreateDietAsync(int clientId, CreateDietDto dto, string coachId);
    Task<List<DietResponseDto>> GetDietsAsync(int clientId, string coachId);

    // AddDietMealAsync: link + optional initial items for dictionary auto-fill
    Task<DietResponseDto?> AddDietMealAsync(
        int clientId, int dietId, string mealName, string? instruction,
        int? parentDietMealId, string coachId,
        string? link = null, List<CreateMealItemDto>? initialItems = null);

    Task<DietResponseDto?> UpdateDietMealAsync(
        int clientId, int dietMealId, string mealName, string? instruction,
        string? link, string coachId);

    Task<(bool Success, string Message, DietResponseDto? UpdatedDiet)> DeleteDietMealAsync(
        int clientId, int dietMealId, string coachId);

    Task<DietResponseDto?> AddMealItemAsync(int clientId, int dietMealId, CreateMealItemDto dto, string coachId);
    Task<(bool Success, string Message)> DeleteDietAsync(int clientId, int dietId, string coachId);
    Task<(bool Success, string Message)> DeleteMealItemAsync(int mealId, string coachId);
    Task<(bool Success, string Message)> UpdateMealItemAsync(int mealId, CreateMealItemDto dto, string coachId);
}