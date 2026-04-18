// Services/IClientService.cs
using FitPanel.DTOs.Client;

namespace FitPanel.Services;

public interface IClientService
{
    Task<(bool Success, string Message)> RenewSubscriptionAsync(int clientId, int months, string coachId);
    Task<ClientResponseDto> CreateClientAsync(CreateClientDto dto, string coachId);
    Task<List<ClientResponseDto>> GetMyClientsAsync(string coachId);
    Task<ClientResponseDto?> GetClientByIdAsync(int clientId, string coachId);
    Task<(bool Success, string Message)> UpdateClientAsync(int clientId, UpdateClientDto dto, string coachId);
    Task<(bool Success, string Message)> DeleteClientAsync(int clientId, string coachId);
}