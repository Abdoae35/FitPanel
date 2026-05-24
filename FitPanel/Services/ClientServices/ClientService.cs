// Services/ClientService.cs
using FitPanel.Data;
using FitPanel.Data.Models;
using FitPanel.DTOs.Client;
using Microsoft.EntityFrameworkCore;

namespace FitPanel.Services;

public class ClientService : IClientService
{
    private readonly FitPanelDbContext _db;

    public ClientService(FitPanelDbContext db)
    {
        _db = db;
    }

public async Task<(bool Success, string Message)> RenewSubscriptionAsync(
    int clientId, int months, string coachId)
{
    var client = await _db.Clients
        .FirstOrDefaultAsync(c => c.Id == clientId && c.CoachId == coachId);

    if (client == null)
        return (false, "Client not found.");

    // Update subscription
    client.StartDate = DateTime.UtcNow;
    client.SubscriptionDurationPerMonth = months;
    client.EndDate = client.StartDate.AddDays(months * 30);

    await _db.SaveChangesAsync();

    return (true, "Subscription renewed successfully.");
}
    public async Task<ClientResponseDto> CreateClientAsync(CreateClientDto dto, string coachId)
    {
        var client = new Client
        {
            Name = dto.Name,
            Weight = dto.Weight,
            Bmr = dto.Bmr,
            Goal = dto.Goal,
            PhoneNumber = dto.PhoneNumber,
            SubscriptionDurationPerMonth = dto.SubscriptionDurationPerMonth,
            InbodyLink = dto.InbodyLink,
            FromPicLink = dto.FromPicLink,
            ToPicLink = dto.ToPicLink,
            CoachId = coachId,
            CreatedAt = DateTime.UtcNow,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(dto.SubscriptionDurationPerMonth * 30)
        };

        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        return await GetClientByIdAsync(client.Id, coachId)
               ?? throw new Exception("Failed to retrieve created client.");
    }

    public async Task<List<ClientResponseDto>> GetMyClientsAsync(string coachId)
    {
        return await _db.Clients
            .Where(c => c.CoachId == coachId)
            .Include(c => c.Coach)
            .Select(c => new ClientResponseDto(
                c.Id,
                c.Name,
                c.Weight,
                c.Bmr,
                c.SubscriptionDurationPerMonth,
                c.InbodyLink,
                c.FromPicLink,
                c.ToPicLink,
                c.CreatedAt,
                c.Coach.FullName,
                c.StartDate,
                c.EndDate,
                c.Goal,
                c.PhoneNumber
            ))
            .ToListAsync();
    }

    public async Task<ClientResponseDto?> GetClientByIdAsync(int clientId, string coachId)
    {
        return await _db.Clients
            .Where(c => c.Id == clientId && c.CoachId == coachId)
            .Include(c => c.Coach)
            .Select(c => new ClientResponseDto(
                c.Id,
                c.Name,
                c.Weight,
                c.Bmr,
                c.SubscriptionDurationPerMonth,
                c.InbodyLink,
                c.FromPicLink,
                c.ToPicLink,
                c.CreatedAt,
                c.Coach.FullName,
                c.StartDate,
                c.EndDate,
                c.Goal,
                c.PhoneNumber
            ))
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string Message)> UpdateClientAsync(
        int clientId, UpdateClientDto dto, string coachId)
    {
        var client = await _db.Clients
            .FirstOrDefaultAsync(c => c.Id == clientId && c.CoachId == coachId);

        if (client == null)
            return (false, "Client not found.");

        if (dto.Name != null) client.Name = dto.Name;
        if (dto.Weight != null) client.Weight = dto.Weight.Value;
        if (dto.Bmr != null) client.Bmr = dto.Bmr.Value;
        if (dto.SubscriptionDurationPerMonth != null)
            client.SubscriptionDurationPerMonth = dto.SubscriptionDurationPerMonth.Value;
        if (dto.InbodyLink != null) client.InbodyLink = dto.InbodyLink;
        if (dto.FromPicLink != null) client.FromPicLink = dto.FromPicLink;
        if (dto.ToPicLink != null) client.ToPicLink = dto.ToPicLink;
        if (dto.Goal != null) client.Goal = dto.Goal;
        if (dto.PhoneNumber != null) client.PhoneNumber = dto.PhoneNumber;

        await _db.SaveChangesAsync();
        return (true, "Client updated.");
    }

    public async Task<(bool Success, string Message)> DeleteClientAsync(
        int clientId, string coachId)
    {
        var client = await _db.Clients
            .FirstOrDefaultAsync(c => c.Id == clientId && c.CoachId == coachId);

        if (client == null)
            return (false, "Client not found.");

        _db.Clients.Remove(client);
        await _db.SaveChangesAsync();
        return (true, "Client deleted.");
    }
}