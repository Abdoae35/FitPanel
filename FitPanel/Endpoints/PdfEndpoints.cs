using FitPanel.Data;
using FitPanel.Services;
using Microsoft.AspNetCore.Identity;

namespace FitPanel.Endpoints;

public static class PdfEndpoints
{
    public static void MapPdfEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/clients/{clientId:int}/pdf")
            .WithTags("PDF")
            .RequireAuthorization("CoachOnly");

        // GET /api/clients/{clientId}/pdf/workout/{workoutId}
        group.MapGet("/workout/{workoutId:int}", async (
            int clientId,
            int workoutId,
            HttpContext http,
            UserManager<PanelUser> userManager,
            IPdfService pdfService) =>
        {
            var coachId = userManager.GetUserId(http.User)!;
            var bytes = await pdfService.GenerateWorkoutPdfAsync(
                clientId, workoutId, coachId);

            return bytes is null
                ? Results.NotFound(new { message = "Workout not found." })
                : Results.File(bytes, "application/pdf",
                    $"workout_{clientId}_{workoutId}.pdf");
        });

        // GET /api/clients/{clientId}/pdf/diet/{dietId}
        group.MapGet("/diet/{dietId:int}", async (
            int clientId,
            int dietId,
            HttpContext http,
            UserManager<PanelUser> userManager,
            IPdfService pdfService) =>
        {
            var coachId = userManager.GetUserId(http.User)!;
            var bytes = await pdfService.GenerateDietPdfAsync(
                clientId, dietId, coachId);

            return bytes is null
                ? Results.NotFound(new { message = "Diet not found." })
                : Results.File(bytes, "application/pdf",
                    $"diet_{clientId}_{dietId}.pdf");
        });
    }
}