// Services/IPdfService.cs
namespace FitPanel.Services;

public interface IPdfService
{
    Task<byte[]?> GenerateWorkoutPdfAsync(int clientId, int workoutId, string coachId);
    Task<byte[]?> GenerateDietPdfAsync(int clientId, int dietId, string coachId);
}