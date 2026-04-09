// Endpoints/WorkoutEndpoints.cs
using FitPanel.DTOs.Workout;
using FitPanel.Services;
using Microsoft.AspNetCore.Identity;
using FitPanel.Data;

namespace FitPanel.Endpoints;

public static class WorkoutEndpoints
{
    public static void MapWorkoutEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/clients/{clientId:int}/workouts")
            .WithTags("Workouts")
            .RequireAuthorization("CoachOnly");

        // GET /api/clients/{clientId}/workouts
        group.MapGet("/", async (
            int clientId,
            HttpContext http,
            UserManager<PanelUser> userManager,
            IWorkoutService workoutService) =>
        {
            var coachId = userManager.GetUserId(http.User)!;
            var workouts = await workoutService.GetWorkoutsAsync(clientId, coachId);
            return Results.Ok(workouts);
        });

        // POST /api/clients/{clientId}/workouts
        group.MapPost("/", async (
            int clientId,
            CreateWorkoutDto dto,
            HttpContext http,
            UserManager<PanelUser> userManager,
            IWorkoutService workoutService) =>
        {
            var coachId = userManager.GetUserId(http.User)!;
            var workout = await workoutService.CreateWorkoutAsync(clientId, dto, coachId);
            return Results.Created($"/api/clients/{clientId}/workouts/{workout.Id}", workout);
        });

        // POST /api/clients/{clientId}/workouts/{workoutId}/days
        group.MapPost("/{workoutId:int}/days", async (
            int clientId,
            int workoutId,
            CreateWorkoutDayDto dto,
            HttpContext http,
            UserManager<PanelUser> userManager,
            IWorkoutService workoutService) =>
        {
            var coachId = userManager.GetUserId(http.User)!;
            var day = await workoutService.AddWorkoutDayAsync(clientId, workoutId, dto, coachId);
            return day is null ? Results.NotFound() : Results.Created($"/api/clients/{clientId}/workouts/{workoutId}/days/{day.Id}", day);
        });

        // POST /api/clients/{clientId}/workouts/{workoutId}/days/{dayId}/exercises
        group.MapPost("/{workoutId:int}/days/{dayId:int}/exercises", async (
            int clientId,
            int workoutId,
            int dayId,
            CreateExerciseDto dto,
            HttpContext http,
            UserManager<PanelUser> userManager,
            IWorkoutService workoutService) =>
        {
            var coachId = userManager.GetUserId(http.User)!;
            var exercise = await workoutService.AddExerciseAsync(clientId, workoutId, dayId, dto, coachId);
            return exercise is null ? Results.NotFound() : Results.Created($"/api/clients/{clientId}/workouts/{workoutId}/days/{dayId}/exercises/{exercise.Id}", exercise);
        });

        // DELETE /api/clients/{clientId}/workouts/{workoutId}
        group.MapDelete("/{workoutId:int}", async (
            int clientId,
            int workoutId,
            HttpContext http,
            UserManager<PanelUser> userManager,
            IWorkoutService workoutService) =>
        {
            var coachId = userManager.GetUserId(http.User)!;
            var (success, message) = await workoutService.DeleteWorkoutAsync(clientId, workoutId, coachId);
            return success ? Results.Ok(new { message }) : Results.NotFound(new { message });
        });

                    // POST /api/clients/{clientId}/workouts/{workoutId}/days/{dayId}/cardio
            group.MapPost("/{workoutId:int}/days/{dayId:int}/cardio", async (
                int clientId,
                int workoutId,
                int dayId,
                CreateCardioDto dto,
                HttpContext http,
                UserManager<PanelUser> userManager,
                IWorkoutService workoutService) =>
            {
                var coachId = userManager.GetUserId(http.User)!;
                var cardio = await workoutService.AddCardioAsync(
                    clientId, workoutId, dayId, dto, coachId);
                return cardio is null
                    ? Results.NotFound()
                    : Results.Created(
                        $"/api/clients/{clientId}/workouts/{workoutId}/days/{dayId}/cardio",
                        cardio);
            });

            // DELETE /api/clients/{clientId}/workouts/{workoutId}/days/{dayId}/cardio
            group.MapDelete("/{workoutId:int}/days/{dayId:int}/cardio", async (
                int clientId,
                int workoutId,
                int dayId,
                HttpContext http,
                UserManager<PanelUser> userManager,
                IWorkoutService workoutService) =>
            {
                var coachId = userManager.GetUserId(http.User)!;
                var (success, message) = await workoutService.DeleteCardioAsync(
                    clientId, workoutId, dayId, coachId);
                return success
                    ? Results.Ok(new { message })
                    : Results.NotFound(new { message });
            });

            // DELETE /api/clients/{clientId}/workouts/{workoutId}/days/{dayId}/exercises/{exerciseId}
           group.MapDelete("/exercises/{exerciseId:int}", async (
            int exerciseId,
            HttpContext http,
            UserManager<PanelUser> userManager,
            IWorkoutService workoutService) =>
        {
            var coachId = userManager.GetUserId(http.User)!;

            var (success, message) =
                await workoutService.DeleteExerciseAsync(exerciseId, coachId);

            return success
                ? Results.Ok(new { message })
                : Results.NotFound(new { message });
        });



    }
}