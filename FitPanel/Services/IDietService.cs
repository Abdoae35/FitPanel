// Services/IDietService.cs
using FitPanel.DTOs.Diet;

namespace FitPanel.Services;

public interface IDietService
{
    Task<DietResponseDto> CreateDietAsync(int clientId, CreateDietDto dto, string coachId);
    Task<List<DietResponseDto>> GetDietsAsync(int clientId, string coachId);
    Task<DietResponseDto?> AddMealItemAsync(int clientId, int dietId, CreateMealItemDto dto, string coachId);
    Task<(bool Success, string Message)> DeleteMealItemAsync(int mealItemId);
    Task<(bool Success, string Message)> DeleteDietAsync(int clientId, int dietId, string coachId);

}