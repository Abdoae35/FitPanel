// Endpoints/DietEndpoints.cs
using FitPanel.DTOs.Diet;
using FitPanel.Services;
using Microsoft.AspNetCore.Identity;
using FitPanel.Data;

namespace FitPanel.Endpoints;

public static class DietEndpoints
{
    public static void MapDietEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/clients/{clientId:int}/diets")
            .WithTags("Diets")
            .RequireAuthorization("CoachOnly");

        // GET /api/clients/{clientId}/diets
        group.MapGet("/", async (
            int clientId,
            HttpContext http,
            UserManager<PanelUser> userManager,
            IDietService dietService) =>
        {
            var coachId = userManager.GetUserId(http.User)!;
            var diets = await dietService.GetDietsAsync(clientId, coachId);
            return Results.Ok(diets);
        });

        // POST /api/clients/{clientId}/diets
        group.MapPost("/", async (
            int clientId,
            CreateDietDto dto,
            HttpContext http,
            UserManager<PanelUser> userManager,
            IDietService dietService) =>
        {
            var coachId = userManager.GetUserId(http.User)!;
            var diet = await dietService.CreateDietAsync(clientId, dto, coachId);
            return Results.Created($"/api/clients/{clientId}/diets/{diet.Id}", diet);
        });

        // POST /api/clients/{clientId}/diets/{dietId}/meals
        group.MapPost("/{dietId:int}/meals", async (
            int clientId,
            int dietId,
            CreateMealItemDto dto,
            HttpContext http,
            UserManager<PanelUser> userManager,
            IDietService dietService) =>
        {
            
            var coachId = userManager.GetUserId(http.User)!;
    

            var result = await dietService.AddMealItemAsync(clientId, dietId, dto, coachId);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        // DELETE /api/clients/{clientId}/diets/{dietId}
        group.MapDelete("/{dietId:int}", async (
            int clientId,
            int dietId,
            HttpContext http,
            UserManager<PanelUser> userManager,
            IDietService dietService) =>
        {
            var coachId = userManager.GetUserId(http.User)!;
            var (success, message) = await dietService.DeleteDietAsync(clientId, dietId, coachId);
            return success ? Results.Ok(new { message }) : Results.NotFound(new { message });
        });


        // DELETE /api/clients/{clientId}/diets/{dietId}/meals/{mealId}
        group.MapDelete("/{dietId:int}/meals/{mealId:int}", async (
            int clientId,
            int dietId,
            int mealId,
            HttpContext http,
            UserManager<PanelUser> userManager,
            IDietService dietService) =>
        {
            var coachId = userManager.GetUserId(http.User)!;
            var (success, message) = await dietService.DeleteMealItemAsync(mealId);
            return success ? Results.Ok(new { message }) : Results.NotFound(new { message });
        });
    }
}