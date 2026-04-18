// Services/IAlternativeService.cs
using FitPanel.DTOs.Diet;

namespace FitPanel.Services;

public interface IAlternativeService
{
    Task<AlternativeResponeDto> AddAlternativeAsync(int mealItemId, CreateAlternativeDto dto, string coachId);
    Task<(bool Success, string Message)> UpdateAlternativeAsync(int alternativeId, CreateAlternativeDto dto, string coachId);
    Task<(bool Success, string Message)> DeleteAlternativeAsync(int alternativeId, string coachId);
}