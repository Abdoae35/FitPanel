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
        // Creates a named DietMeal container (e.g. "Breakfast") — supports Link and pre-populated items
        group.MapPost("/{dietId:int}/meals", async (
            int clientId,
            int dietId,
            CreateDietMealDto dto,
            HttpContext http,
            UserManager<PanelUser> userManager,
            IDietService dietService) =>
        {
            var coachId = userManager.GetUserId(http.User)!;
            var result = await dietService.AddDietMealAsync(
                clientId, dietId, dto.Name, dto.Instruction,
                dto.ParentDietMealId, coachId, dto.Link, dto.InitialItems);
            return result is null
                ? Results.NotFound()
                : Results.Created($"/api/clients/{clientId}/diets/{dietId}/meals", result);
        });

        // FIX #1: Route corrected — POST /api/clients/{clientId}/diets/{dietId}/meals/{dietMealId}/items
        // Adds a MealItem (ingredient) into a specific DietMeal.
        // Previously the route was /{dietId}/meals and it incorrectly passed dietId as dietMealId.
        group.MapPost("/{dietId:int}/meals/{dietMealId:int}/items", async (
            int clientId,
            int dietId,
            int dietMealId,
            CreateMealItemDto dto,
            HttpContext http,
            UserManager<PanelUser> userManager,
            IDietService dietService) =>
        {
            var coachId = userManager.GetUserId(http.User)!;
            var result = await dietService.AddMealItemAsync(clientId, dietMealId, dto, coachId);
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

        // FIX #2: DELETE now passes coachId for ownership verification
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
            var (success, message) = await dietService.DeleteMealItemAsync(mealId, coachId);
            return success ? Results.Ok(new { message }) : Results.NotFound(new { message });
        });

        // FIX #3: PUT now passes coachId for ownership verification
        // PUT /api/clients/{clientId}/diets/{dietId}/meals/{mealId}
        group.MapPut("/{dietId:int}/meals/{mealId:int}", async (
            int clientId,
            int dietId,
            int mealId,
            CreateMealItemDto dto,
            HttpContext http,
            UserManager<PanelUser> userManager,
            IDietService dietService) =>
        {
            var coachId = userManager.GetUserId(http.User)!;
            var (success, message) = await dietService.UpdateMealItemAsync(mealId, dto, coachId);
            return success ? Results.Ok(new { message }) : Results.NotFound(new { message });
        });
    }
}