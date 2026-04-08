// Services/IClientService.cs
using FitPanel.DTOs.Client;

namespace FitPanel.Services;

public interface IClientService
{
    Task<ClientResponseDto> CreateClientAsync(CreateClientDto dto, string coachId);
    Task<List<ClientResponseDto>> GetMyClientsAsync(string coachId);
    Task<ClientResponseDto?> GetClientByIdAsync(int clientId, string coachId);
    Task<(bool Success, string Message)> UpdateClientAsync(int clientId, UpdateClientDto dto, string coachId);
    Task<(bool Success, string Message)> DeleteClientAsync(int clientId, string coachId);
}