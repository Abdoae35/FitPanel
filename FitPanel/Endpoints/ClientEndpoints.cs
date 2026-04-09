// Endpoints/ClientEndpoints.cs
using FitPanel.DTOs.Client;
using FitPanel.Services;
using Microsoft.AspNetCore.Identity;
using FitPanel.Data;

namespace FitPanel.Endpoints;

public static class ClientEndpoints
{
    public static void MapClientEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/clients")
            .WithTags("Clients")
            .RequireAuthorization("CoachOnly");

        // GET /api/clients — get all my clients
        group.MapGet("/", async (
            HttpContext http,
            UserManager<PanelUser> userManager,
            IClientService clientService) =>
        {
            var coachId = userManager.GetUserId(http.User)!;
            var clients = await clientService.GetMyClientsAsync(coachId);
            return Results.Ok(clients);
        });

        // GET /api/clients/{id}
        group.MapGet("/{id:int}", async (
            int id,
            HttpContext http,
            UserManager<PanelUser> userManager,
            IClientService clientService) =>
        {
            var coachId = userManager.GetUserId(http.User)!;
            var client = await clientService.GetClientByIdAsync(id, coachId);
            return client is null ? Results.NotFound() : Results.Ok(client);
        });
        // POST /api/clients/{id}/renew
group.MapPost("/{id:int}/renew", async (
    int id,
    RenewSubscriptionDto dto,
    HttpContext http,
    UserManager<PanelUser> userManager,
    IClientService clientService) =>
{
    var coachId = userManager.GetUserId(http.User)!;

    var (success, message) = await clientService
        .RenewSubscriptionAsync(id, dto.Months, coachId);

    return success
        ? Results.Ok(new { message })
        : Results.NotFound(new { message });
});

        // POST /api/clients
        group.MapPost("/", async (
            CreateClientDto dto,
            HttpContext http,
            UserManager<PanelUser> userManager,
            IClientService clientService) =>
        {
            var coachId = userManager.GetUserId(http.User)!;
            var client = await clientService.CreateClientAsync(dto, coachId);
            return Results.Created($"/api/clients/{client.Id}", client);
        });

        // PUT /api/clients/{id}
        group.MapPut("/{id:int}", async (
            int id,
            UpdateClientDto dto,
            HttpContext http,
            UserManager<PanelUser> userManager,
            IClientService clientService) =>
        {
            var coachId = userManager.GetUserId(http.User)!;
            var (success, message) = await clientService.UpdateClientAsync(id, dto, coachId);
            return success ? Results.Ok(new { message }) : Results.NotFound(new { message });
        });

        // DELETE /api/clients/{id}
        group.MapDelete("/{id:int}", async (
            int id,
            HttpContext http,
            UserManager<PanelUser> userManager,
            IClientService clientService) =>
        {
            var coachId = userManager.GetUserId(http.User)!;
            var (success, message) = await clientService.DeleteClientAsync(id, coachId);
            return success ? Results.Ok(new { message }) : Results.NotFound(new { message });
        });
    }
}