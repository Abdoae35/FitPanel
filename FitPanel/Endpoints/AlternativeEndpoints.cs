// Endpoints/AlternativeEndpoints.cs
using FitPanel.DTOs.Diet;
using FitPanel.Services;
using Microsoft.AspNetCore.Identity;
using FitPanel.Data;

namespace FitPanel.Endpoints;

public static class AlternativeEndpoints
{
    public static void MapAlternativeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/meals/{mealItemId:int}/alternatives")
            .WithTags("Alternatives")
            .RequireAuthorization("CoachOnly");

        // POST /api/meals/{mealItemId}/alternatives
        group.MapPost("/", async (
            int mealItemId,
            CreateAlternativeDto dto,
            HttpContext http,
            UserManager<PanelUser> userManager,
            IAlternativeService alternativeService) =>
        {
            var coachId = userManager.GetUserId(http.User)!;
            var result = await alternativeService.AddAlternativeAsync(mealItemId, dto, coachId);
            return Results.Created(
                $"/api/meals/{mealItemId}/alternatives/{result.Id}", result);
        });

        // PUT /api/meals/{mealItemId}/alternatives/{alternativeId}
        group.MapPut("/{alternativeId:int}", async (
            int mealItemId,
            int alternativeId,
            CreateAlternativeDto dto,
            HttpContext http,
            UserManager<PanelUser> userManager,
            IAlternativeService alternativeService) =>
        {
            var coachId = userManager.GetUserId(http.User)!;
            var (success, message) = await alternativeService
                .UpdateAlternativeAsync(alternativeId, dto, coachId);
            return success
                ? Results.Ok(new { message })
                : Results.NotFound(new { message });
        });

        // DELETE /api/meals/{mealItemId}/alternatives/{alternativeId}
        group.MapDelete("/{alternativeId:int}", async (
            int mealItemId,
            int alternativeId,
            HttpContext http,
            UserManager<PanelUser> userManager,
            IAlternativeService alternativeService) =>
        {
            var coachId = userManager.GetUserId(http.User)!;
            var (success, message) = await alternativeService
                .DeleteAlternativeAsync(alternativeId, coachId);
            return success
                ? Results.Ok(new { message })
                : Results.NotFound(new { message });
        });
    }
}