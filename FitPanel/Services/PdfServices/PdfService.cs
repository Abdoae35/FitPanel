using FitPanel.Data;
using FitPanel.Data.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace FitPanel.Services;

public class PdfService : IPdfService
{
    private readonly FitPanelDbContext _db;
    private readonly string _webRootPath;
    private readonly IConfiguration _config;

    public PdfService(FitPanelDbContext db, IWebHostEnvironment env, IConfiguration config)
    {
        _db = db;
        _webRootPath = env.WebRootPath;
        _config = config;
    }

    // ── PUBLIC METHODS ────────────────────────────────────────────
    public async Task<byte[]?> GenerateWorkoutPdfAsync(int clientId, int workoutId, string coachId)
    {
        var workout = await _db.WorkOuts
            .Include(w => w.Client)
                .ThenInclude(c => c.Coach)
            .Include(w => w.WorkOutDays)
                .ThenInclude(d => d.ExcerciseItems)
            .Include(w => w.WorkOutDays)
                .ThenInclude(d => d.Cardio)
            .FirstOrDefaultAsync(w =>
                w.Id == workoutId &&
                w.ClientId == clientId &&
                w.Client.CoachId == coachId);

        if (workout == null) return null;
        var html = BuildWorkoutHtml(workout);
        return await RenderToPdfAsync(html);
    }

    public async Task<byte[]?> GenerateDietPdfAsync(int clientId, int dietId, string coachId)
    {
        var diet = await _db.Diets
            .Include(d => d.Client)
                .ThenInclude(c => c.Coach)
            .Include(d => d.DietMeals)
                .ThenInclude(dm => dm.MealItems)
                    .ThenInclude(m => m.AlternativeItems)
            .FirstOrDefaultAsync(d =>
                d.Id == dietId &&
                d.ClientId == clientId &&
                d.Client.CoachId == coachId);

        if (diet == null) return null;
        var html = BuildDietHtml(diet);
        return await RenderToPdfAsync(html);
    }

    // ── PDFSHIFT CLOUD RENDERER ──────────────────────────────────
    private async Task<byte[]> RenderToPdfAsync(string html)
    {
        var apiKey = _config["PdfShift:ApiKey"];
        if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_PDFSHIFT_API_KEY_HERE")
        {
            throw new InvalidOperationException("PDFShift API key is missing or not configured. Please add your PDFShift ApiKey in appsettings.json.");
        }

        using var client = new HttpClient();
        
        // PDFShift uses the custom X-API-Key HTTP header for authentication
        client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

        var requestBody = new
        {
            source = html,
            format = "A4",
            margin = new { top = "0px", right = "0px", bottom = "0px", left = "0px" }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("https://api.pdfshift.io/v3/convert/pdf", content);
        if (!response.IsSuccessStatusCode)
        {
            var errorMsg = await response.Content.ReadAsStringAsync();
            throw new Exception($"PDFShift PDF generation failed: {response.StatusCode} - {errorMsg}");
        }

        return await response.Content.ReadAsByteArrayAsync();
    }

    // ── WORKOUT HTML BUILDER ──────────────────────────────────────
    private string BuildWorkoutHtml(WorkOut workout)
    {
        var client     = workout.Client;
        var coach      = client.Coach;
        var coachName  = coach?.FullName ?? "الكوتش";
        var coachEmail = coach?.Email ?? "";
        var coachPhone = coach?.PhoneNumber ?? "";
        var instagram  = coach?.InstagramUsername ?? "";
        var instagramLink = coach?.InstagramLink ?? "#";
        
        var photoBase64 = GetCoachPhotoBase64(coach?.ProfilePicture);
        var days = workout.WorkOutDays.OrderBy(d => d.Id).ToList();

        var pagesBuilder = new StringBuilder();
        for (int i = 0; i < days.Count; i++)
        {
            pagesBuilder.Append(WorkoutDayPage(days[i], client.Name, coachEmail, coachPhone, instagram, instagramLink));
        }

        return $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
          <title>Premium Workout Plan</title>
          <style>
            {SharedCss("#ea2127", "rgba(234, 33, 39, 0.25)", "#ff0066")}
          </style>
        </head>
        <body>
          {WorkoutCover(client, coachName, coachEmail, coachPhone, instagram, instagramLink, photoBase64)}
          {pagesBuilder}
        </body>
        </html>
        """;
    }

    // ── DIET HTML BUILDER ─────────────────────────────────────────
    private string BuildDietHtml(Diet diet)
    {
        var client     = diet.Client;
        var coach      = client.Coach;
        var coachName  = coach?.FullName ?? "الكوتش";
        var coachEmail = coach?.Email ?? "";
        var coachPhone = coach?.PhoneNumber ?? "";
        var instagram  = coach?.InstagramUsername ?? "";
        var instagramLink = coach?.InstagramLink ?? "#";

        var photoBase64 = GetCoachPhotoBase64(coach?.ProfilePicture);
        var meals = diet.DietMeals.SelectMany(dm => dm.MealItems).ToList();
        int totalCal     = meals.Sum(m => m.Calories);
        int totalProtein = meals.Sum(m => m.Protein);
        int totalCarbs   = meals.Sum(m => m.Carbs);
        int totalFats    = meals.Sum(m => m.Fats);

        int totalGrams = totalProtein + totalCarbs + totalFats;
        int pPct = totalGrams > 0 ? (int)Math.Round((double)totalProtein / totalGrams * 100) : 0;
        int cPct = totalGrams > 0 ? (int)Math.Round((double)totalCarbs / totalGrams * 100) : 0;
        int fPct = totalGrams > 0 ? (int)Math.Round((double)totalFats / totalGrams * 100) : 0;

        return $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
          <title>Premium Diet Plan</title>
          <style>
            {SharedCss("#00FF88", "rgba(0, 255, 136, 0.25)", "#FF0066")}
          </style>
        </head>
        <body>
          {DietCover(client, coachName, coachEmail, coachPhone, instagram, instagramLink, photoBase64)}
          {DietContentPage(diet, client.Name, coachEmail, coachPhone, instagram, instagramLink, totalCal, totalProtein, totalCarbs, totalFats, pPct, cPct, fPct)}
        </body>
        </html>
        """;
    }

    // ── COVER PAGES ───────────────────────────────────────────────
    private static string WorkoutCover(
        Client client, string coachName, string coachEmail, string coachPhone, string instagram, string instagramLink, string photoBase64) => $"""
        <div class="a4-page">
          <div class="diagonal-overlay"></div>
          <div class="accent-bar" style="top: 0; left: 0; width: 60%;"></div>

          <div class="content-wrapper">
            <div class="cover-grid">
              <!-- Left Column: Coach Photo -->
              <div class="cover-photo-section">
                <img src="{photoBase64}" alt="Coach Photo" class="coach-photo">
              </div>

              <!-- Right Column: Coach Info -->
              <div class="cover-info-section">
                <div>
                  <h1 class="display-massive" style="font-size: 38px; line-height: 1.1; margin-bottom: 20px; color: var(--coach-white);">
                    EVERY WORKOUT IS STEP FORWARD TO YOUR GOAL
                  </h1>
                  <p class="cover-specialty" style="margin-bottom: 20px;">
                    <a href="{instagramLink}" target="_blank" style="color: var(--coach-primary); text-decoration: none; font-size: 20px; font-weight: 700; display: inline-flex; align-items: center; gap: 8px;">
                      Dr. {coachName}
                      <svg style="width: 18px; height: 18px; fill: var(--coach-primary);" viewBox="0 0 24 24">
                        <path d="M12 2.163c3.204 0 3.584.012 4.85.07 3.252.148 4.771 1.691 4.919 4.919.058 1.265.069 1.645.069 4.849 0 3.205-.012 3.584-.069 4.849-.149 3.225-1.664 4.771-4.919 4.919-1.266.058-1.644.07-4.85.07-3.204 0-3.584-.012-4.849-.07-3.26-.149-4.771-1.699-4.919-4.92-.058-1.265-.07-1.644-.07-4.849 0-3.204.013-3.583.07-4.849.149-3.227 1.664-4.771 4.919-4.919 1.266-.057 1.645-.069 4.849-.069zm0-2c-3.259 0-3.667.014-4.947.072-4.358.2-6.78 2.618-6.98 6.98-.059 1.281-.073 1.689-.073 4.948 0 3.259.014 3.668.072 4.948.2 4.358 2.618 6.78 6.98 6.98 1.281.058 1.689.072 4.948.072 3.259 0 3.668-.014 4.948-.072 4.354-.2 6.782-2.618 6.979-6.98.059-1.28.073-1.689.073-4.948 0-3.259-.014-3.667-.072-4.947-.196-4.354-2.617-6.78-6.979-6.98-1.281-.059-1.69-.073-4.949-.073zm0 5.838c-3.403 0-6.162 2.759-6.162 6.162s2.759 6.163 6.162 6.163 6.162-2.759 6.162-6.163c0-3.403-2.759-6.162-6.162-6.162zm0 10.162c-2.209 0-4-1.79-4-4 0-2.209 1.791-4 4-4s4 1.791 4 4c0 2.21-1.791 4-4 4zm6.406-11.845c-.796 0-1.441.645-1.441 1.44s.645 1.44 1.441 1.44c.795 0 1.439-.645 1.439-1.44s-.644-1.44-1.439-1.44z"/>
                      </svg>
                    </a>
                  </p>
                </div>

                <div class="cover-keys-list" style="margin-bottom: 28px;">
                  <div style="font-family: var(--coach-font-body); font-size: 14px; font-weight: 800; color: var(--coach-primary); letter-spacing: 1px; text-transform: uppercase; margin-bottom: 10px;">OUR KEYS TO SUCCESS</div>
                  <ul style="list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 6px; font-size: 12px; font-weight: 500; color: var(--coach-gray-light);">
                    <li style="display: flex; align-items: center; gap: 8px;"><span style="color: var(--coach-primary);">⚡</span> WORKOUT</li>
                    <li style="display: flex; align-items: center; gap: 8px;"><span style="color: var(--coach-primary);">⚡</span> ABS</li>
                    <li style="display: flex; align-items: center; gap: 8px;"><span style="color: var(--coach-primary);">⚡</span> NUTRITION</li>
                    <li style="display: flex; align-items: center; gap: 8px;"><span style="color: var(--coach-primary);">⚡</span> CARDIO</li>
                    <li style="display: flex; align-items: center; gap: 8px;"><span style="color: var(--coach-primary);">⚡</span> SUPPLEMENTATION</li>
                  </ul>
                  <div style="margin-top: 14px; font-style: italic; font-size: 12px; color: var(--coach-white); font-weight: 600;">
                    "All are keys to reach our goal"
                  </div>
                </div>

                <div style="margin-top: auto;">
                  <span class="doc-title-label">PERSONALIZED WORKOUT PLAN</span>
                </div>

                <div class="client-meta" style="margin-top: 16px;">
                  <div class="client-meta-row">
                    <span class="client-meta-label">CLIENT NAME</span>
                    <span class="client-meta-value">{client.Name}</span>
                  </div>
                  <div class="client-meta-row">
                    <span class="client-meta-label">PLAN START DATE</span>
                    <span class="client-meta-value">{client.StartDate:MMMM dd, yyyy}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <footer class="footer">
            <div>
              <a href="{instagramLink}" target="_blank" style="color: var(--coach-gray-light); text-decoration: none; display: flex; align-items: center; gap: 6px;">
                <svg style="width: 14px; height: 14px; fill: var(--coach-gray-light);" viewBox="0 0 24 24">
                  <path d="M12 2.163c3.204 0 3.584.012 4.85.07 3.252.148 4.771 1.691 4.919 4.919.058 1.265.069 1.645.069 4.849 0 3.205-.012 3.584-.069 4.849-.149 3.225-1.664 4.771-4.919 4.919-1.266.058-1.644.07-4.85.07-3.204 0-3.584-.012-4.849-.07-3.26-.149-4.771-1.699-4.919-4.92-.058-1.265-.07-1.644-.07-4.849 0-3.204.013-3.583.07-4.849.149-3.227 1.664-4.771 4.919-4.919 1.266-.057 1.645-.069 4.849-.069zm0-2c-3.259 0-3.667.014-4.947.072-4.358.2-6.78 2.618-6.98 6.98-.059 1.281-.073 1.689-.073 4.948 0 3.259.014 3.668.072 4.948.2 4.358 2.618 6.78 6.98 6.98 1.281.058 1.689.072 4.948.072 3.259 0 3.668-.014 4.948-.072 4.354-.2 6.782-2.618 6.979-6.98.059-1.28.073-1.689.073-4.948 0-3.259-.014-3.667-.072-4.947-.196-4.354-2.617-6.78-6.979-6.98-1.281-.059-1.69-.073-4.949-.073zm0 5.838c-3.403 0-6.162 2.759-6.162 6.162s2.759 6.163 6.162 6.163 6.162-2.759 6.162-6.163c0-3.403-2.759-6.162-6.162-6.162zm0 10.162c-2.209 0-4-1.79-4-4 0-2.209 1.791-4 4-4s4 1.791 4 4c0 2.21-1.791 4-4 4zm6.406-11.845c-.796 0-1.441.645-1.441 1.44s.645 1.44 1.441 1.44c.795 0 1.439-.645 1.439-1.44s-.644-1.44-1.439-1.44z"/>
                </svg>
                <span>@{instagram}</span>
              </a>
            </div>
            <div><span>✉</span><span>{coachEmail}</span></div>
            <div><span>📞</span><span>{coachPhone}</span></div>
          </footer>
        </div>
        """;

    private static string DietCover(
        Client client, string coachName, string coachEmail, string coachPhone, string instagram, string instagramLink, string photoBase64) => $"""
        <div class="a4-page">
          <div class="diagonal-overlay"></div>
          <div class="accent-bar" style="top: 0; left: 0; width: 60%;"></div>

          <div class="content-wrapper">
            <div class="cover-grid">
              <!-- Left Column: Coach Photo -->
              <div class="cover-photo-section">
                <img src="{photoBase64}" alt="Coach Photo" class="coach-photo">
              </div>

              <!-- Right Column: Coach Info -->
              <div class="cover-info-section">
                <div>
                  <h1 class="display-massive" style="font-size: 38px; line-height: 1.1; margin-bottom: 20px; color: var(--coach-white);">
                    EVERY WORKOUT IS STEP FORWARD TO YOUR GOAL
                  </h1>
                  <p class="cover-specialty" style="margin-bottom: 20px;">
                    <a href="{instagramLink}" target="_blank" style="color: var(--coach-primary); text-decoration: none; font-size: 20px; font-weight: 700; display: inline-flex; align-items: center; gap: 8px;">
                      Dr. {coachName}
                      <svg style="width: 18px; height: 18px; fill: var(--coach-primary);" viewBox="0 0 24 24">
                        <path d="M12 2.163c3.204 0 3.584.012 4.85.07 3.252.148 4.771 1.691 4.919 4.919.058 1.265.069 1.645.069 4.849 0 3.205-.012 3.584-.069 4.849-.149 3.225-1.664 4.771-4.919 4.919-1.266.058-1.644.07-4.85.07-3.204 0-3.584-.012-4.849-.07-3.26-.149-4.771-1.699-4.919-4.92-.058-1.265-.07-1.644-.07-4.849 0-3.204.013-3.583.07-4.849.149-3.227 1.664-4.771 4.919-4.919 1.266-.057 1.645-.069 4.849-.069zm0-2c-3.259 0-3.667.014-4.947.072-4.358.2-6.78 2.618-6.98 6.98-.059 1.281-.073 1.689-.073 4.948 0 3.259.014 3.668.072 4.948.2 4.358 2.618 6.78 6.98 6.98 1.281.058 1.689.072 4.948.072 3.259 0 3.668-.014 4.948-.072 4.354-.2 6.782-2.618 6.979-6.98.059-1.28.073-1.689.073-4.948 0-3.259-.014-3.667-.072-4.947-.196-4.354-2.617-6.78-6.979-6.98-1.281-.059-1.69-.073-4.949-.073zm0 5.838c-3.403 0-6.162 2.759-6.162 6.162s2.759 6.163 6.162 6.163 6.162-2.759 6.162-6.163c0-3.403-2.759-6.162-6.162-6.162zm0 10.162c-2.209 0-4-1.79-4-4 0-2.209 1.791-4 4-4s4 1.791 4 4c0 2.21-1.791 4-4 4zm6.406-11.845c-.796 0-1.441.645-1.441 1.44s.645 1.44 1.441 1.44c.795 0 1.439-.645 1.439-1.44s-.644-1.44-1.439-1.44z"/>
                      </svg>
                    </a>
                  </p>
                </div>

                <div class="cover-keys-list" style="margin-bottom: 28px;">
                  <div style="font-family: var(--coach-font-body); font-size: 14px; font-weight: 800; color: var(--coach-primary); letter-spacing: 1px; text-transform: uppercase; margin-bottom: 10px;">OUR KEYS TO SUCCESS</div>
                  <ul style="list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 6px; font-size: 12px; font-weight: 500; color: var(--coach-gray-light);">
                    <li style="display: flex; align-items: center; gap: 8px;"><span style="color: var(--coach-primary);">⚡</span> WORKOUT</li>
                    <li style="display: flex; align-items: center; gap: 8px;"><span style="color: var(--coach-primary);">⚡</span> ABS</li>
                    <li style="display: flex; align-items: center; gap: 8px;"><span style="color: var(--coach-primary);">⚡</span> NUTRITION</li>
                    <li style="display: flex; align-items: center; gap: 8px;"><span style="color: var(--coach-primary);">⚡</span> CARDIO</li>
                    <li style="display: flex; align-items: center; gap: 8px;"><span style="color: var(--coach-primary);">⚡</span> SUPPLEMENTATION</li>
                  </ul>
                  <div style="margin-top: 14px; font-style: italic; font-size: 12px; color: var(--coach-white); font-weight: 600;">
                    "All are keys to reach our goal"
                  </div>
                </div>

                <div style="margin-top: auto;">
                  <span class="doc-title-label">PERSONALIZED DIET PLAN</span>
                </div>

                <div class="client-meta" style="margin-top: 16px;">
                  <div class="client-meta-row">
                    <span class="client-meta-label">CLIENT NAME</span>
                    <span class="client-meta-value">{client.Name}</span>
                  </div>
                  <div class="client-meta-row">
                    <span class="client-meta-label">PLAN START DATE</span>
                    <span class="client-meta-value">{client.StartDate:MMMM dd, yyyy}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <footer class="footer">
            <div>
              <a href="{instagramLink}" target="_blank" style="color: var(--coach-gray-light); text-decoration: none; display: flex; align-items: center; gap: 6px;">
                <svg style="width: 14px; height: 14px; fill: var(--coach-gray-light);" viewBox="0 0 24 24">
                  <path d="M12 2.163c3.204 0 3.584.012 4.85.07 3.252.148 4.771 1.691 4.919 4.919.058 1.265.069 1.645.069 4.849 0 3.205-.012 3.584-.069 4.849-.149 3.225-1.664 4.771-4.919 4.919-1.266.058-1.644.07-4.85.07-3.204 0-3.584-.012-4.849-.07-3.26-.149-4.771-1.699-4.919-4.92-.058-1.265-.07-1.644-.07-4.849 0-3.204.013-3.583.07-4.849.149-3.227 1.664-4.771 4.919-4.919 1.266-.057 1.645-.069 4.849-.069zm0-2c-3.259 0-3.667.014-4.947.072-4.358.2-6.78 2.618-6.98 6.98-.059 1.281-.073 1.689-.073 4.948 0 3.259.014 3.668.072 4.948.2 4.358 2.618 6.78 6.98 6.98 1.281.058 1.689.072 4.948.072 3.259 0 3.668-.014 4.948-.072 4.354-.2 6.782-2.618 6.979-6.98.059-1.28.073-1.689.073-4.948 0-3.259-.014-3.667-.072-4.947-.196-4.354-2.617-6.78-6.979-6.98-1.281-.059-1.69-.073-4.949-.073zm0 5.838c-3.403 0-6.162 2.759-6.162 6.162s2.759 6.163 6.162 6.163 6.162-2.759 6.162-6.163c0-3.403-2.759-6.162-6.162-6.162zm0 10.162c-2.209 0-4-1.79-4-4 0-2.209 1.791-4 4-4s4 1.791 4 4c0 2.21-1.791 4-4 4zm6.406-11.845c-.796 0-1.441.645-1.441 1.44s.645 1.44 1.441 1.44c.795 0 1.439-.645 1.439-1.44s-.644-1.44-1.439-1.44z"/>
                </svg>
                <span>@{instagram}</span>
              </a>
            </div>
            <div><span>✉</span><span>{coachEmail}</span></div>
            <div><span>📞</span><span>{coachPhone}</span></div>
          </footer>
        </div>
        """;

    // ── WORKOUT DAY PAGE ──────────────────────────────────────────
    private static string WorkoutDayPage(
        WorkOutDay day, string clientName, string email, string phone, string instagram, string instagramLink)
    {
        var rows = new StringBuilder();
        foreach (var ex in day.ExcerciseItems)
        {
            var exerciseNameHtml = string.IsNullOrEmpty(ex.ExcerciseLink) 
                ? ex.ExerciseName 
                : $"<a href='{ex.ExcerciseLink}' style='color: var(--coach-primary); text-decoration: none; font-weight: 700;'>{ex.ExerciseName} <i style='font-size: 9px; margin-left: 2px;'>🔗</i></a>";

            rows.Append($"""
                <tr>
                  <td class="exercise-name">{exerciseNameHtml}</td>
                  <td style="text-align: center; font-weight: 700;">{ex.Sets}</td>
                  <td style="text-align: center;">{ex.Reps}</td>
                  <td style="text-align: center; color: var(--coach-primary); font-weight: 700;">{ex.RestTime}</td>
                </tr>
                """);
        }

        var cardioSection = "";
        if (day.Cardio != null)
        {
            var c = day.Cardio;
            cardioSection = $"""
                <div class="cardio-section" style="margin-top: auto;">
                  <div class="cardio-header">
                    <div class="cardio-title">🏃 CARDIO SPECIFICATION PROTOCOL</div>
                    <div class="cardio-badge">{c.Intensity}</div>
                  </div>
                  <div class="cardio-grid">
                    <div class="cardio-metric">
                      <div class="cardio-metric-label">TYPE / METHOD</div>
                      <div class="cardio-metric-val">{c.CardioType}</div>
                    </div>
                    <div class="cardio-metric">
                      <div class="cardio-metric-label">DURATION TIME</div>
                      <div class="cardio-metric-val text-primary">{c.DurationMinutes} minutes</div>
                    </div>
                    <div class="cardio-metric">
                      <div class="cardio-metric-label">TARGET CONSTRAINTS</div>
                      <div class="cardio-metric-val">{c.Notes ?? "No constraints"}</div>
                    </div>
                  </div>
                </div>
                """;
        }

        return $"""
        <div class="a4-page">
          <div class="diagonal-overlay"></div>
          <div class="content-wrapper">
            <header class="header">
              <div>
                <h2>DAY <span class="text-primary">{day.Day}</span> <span>{day.DayName}</span></h2>
                <p>FitPanel Elite Coaching Protocols</p>
              </div>
              <div>
                <span class="skew-badge"><span>STRENGTH PHASE</span></span>
              </div>
            </header>

            <div class="table-container">
              <table>
                <thead>
                  <tr>
                    <th style="width: 50%;">MOVEMENT & PATTERN</th>
                    <th style="width: 15%; text-align: center;">SETS</th>
                    <th style="width: 20%; text-align: center;">REPS</th>
                    <th style="width: 15%; text-align: center;">REST</th>
                  </tr>
                </thead>
                <tbody>
                  {rows}
                </tbody>
              </table>
            </div>

            {cardioSection}
          </div>

          <footer class="footer">
            <div>
              <a href="{instagramLink}" target="_blank" style="color: var(--coach-gray-light); text-decoration: none; display: flex; align-items: center; gap: 6px;">
                <svg style="width: 14px; height: 14px; fill: var(--coach-gray-light);" viewBox="0 0 24 24">
                  <path d="M12 2.163c3.204 0 3.584.012 4.85.07 3.252.148 4.771 1.691 4.919 4.919.058 1.265.069 1.645.069 4.849 0 3.205-.012 3.584-.069 4.849-.149 3.225-1.664 4.771-4.919 4.919-1.266.058-1.644.07-4.85.07-3.204 0-3.584-.012-4.849-.07-3.26-.149-4.771-1.699-4.919-4.92-.058-1.265-.07-1.644-.07-4.849 0-3.204.013-3.583.07-4.849.149-3.227 1.664-4.771 4.919-4.919 1.266-.057 1.645-.069 4.849-.069zm0-2c-3.259 0-3.667.014-4.947.072-4.358.2-6.78 2.618-6.98 6.98-.059 1.281-.073 1.689-.073 4.948 0 3.259.014 3.668.072 4.948.2 4.358 2.618 6.78 6.98 6.98 1.281.058 1.689.072 4.948.072 3.259 0 3.668-.014 4.948-.072 4.354-.2 6.782-2.618 6.979-6.98.059-1.28.073-1.689.073-4.948 0-3.259-.014-3.667-.072-4.947-.196-4.354-2.617-6.78-6.979-6.98-1.281-.059-1.69-.073-4.949-.073zm0 5.838c-3.403 0-6.162 2.759-6.162 6.162s2.759 6.163 6.162 6.163 6.162-2.759 6.162-6.163c0-3.403-2.759-6.162-6.162-6.162zm0 10.162c-2.209 0-4-1.79-4-4 0-2.209 1.791-4 4-4s4 1.791 4 4c0 2.21-1.791 4-4 4zm6.406-11.845c-.796 0-1.441.645-1.441 1.44s.645 1.44 1.441 1.44c.795 0 1.439-.645 1.439-1.44s-.644-1.44-1.439-1.44z"/>
                </svg>
                <span>@{instagram}</span>
              </a>
            </div>
            <div><span>✉</span><span>{email}</span></div>
            <div><span>📞</span><span>{phone}</span></div>
          </footer>
        </div>
        """;
    }

    // ── DIET PLAN PAGE ────────────────────────────────────────────
    private static string DietContentPage(
        Diet diet, string clientName, string email, string phone, string instagram, string instagramLink,
        int totalCal, int totalProtein, int totalCarbs, int totalFats,
        int pPct, int cPct, int fPct)
    {
        var mealCards = new StringBuilder();
        foreach (var m in diet.DietMeals)
        {
            string icon = "🍽️";
            var mealNameLower = m.Name.ToLower();
            if (mealNameLower.Contains("breakfast") || mealNameLower.Contains("morning") || mealNameLower.Contains("🍳")) icon = "🍳";
            else if (mealNameLower.Contains("lunch") || mealNameLower.Contains("dinner") || mealNameLower.Contains("afternoon") || mealNameLower.Contains("night")) icon = "🍗";
            else if (mealNameLower.Contains("snack") || mealNameLower.Contains("shake") || mealNameLower.Contains("smoothie") || mealNameLower.Contains("drink")) icon = "🥤";
            else if (mealNameLower.Contains("pre") || mealNameLower.Contains("intra") || mealNameLower.Contains("workout")) icon = "⚡";
            
            var foodItems = new StringBuilder();
            foreach (var item in m.MealItems)
            {
                var portionText = $"{item.Quantity} {item.Unit}";
                var altItemsHtml = new StringBuilder();
                if (item.AlternativeItems != null && item.AlternativeItems.Any())
                {
                    altItemsHtml.Append("<div class='food-alternatives' style='margin-top: 4px; padding-left: 12px; border-left: 2px solid var(--coach-secondary); font-size: 9px; color: var(--coach-gray-light);'>");
                    foreach (var alt in item.AlternativeItems)
                    {
                        altItemsHtml.Append($"<div class='alt-item'>↳ Alternative: <strong>{alt.MealName}</strong> — {alt.Quantity} {alt.Unit} <span style='color: #00D9FF; margin-left: 6px;'>P: {alt.Protein}g</span> <span style='color: #FFD700; margin-left: 4px;'>C: {alt.Carbs}g</span> <span style='color: #FF6B00; margin-left: 4px;'>F: {alt.Fats}g</span></div>");
                    }
                    altItemsHtml.Append("</div>");
                }

                foodItems.Append($"""
                    <div class="food-item" style="padding: 10px 0; border-bottom: 1px solid var(--coach-gray-dark);">
                      <div style="display: flex; justify-content: space-between; align-items: center; width: 100%;">
                        <div class="food-name">{item.MealName}</div>
                        <div class="food-portion" style="color: var(--coach-gray-light); margin-left: auto; padding-right: 20px;">{portionText}</div>
                        <div class="food-macros" style="display: flex; gap: 8px;">
                          <span class="macro-chip protein">P: {item.Protein}g</span>
                          <span class="macro-chip carbs">C: {item.Carbs}g</span>
                          <span class="macro-chip fats">F: {item.Fats}g</span>
                        </div>
                      </div>
                      {altItemsHtml}
                    </div>
                    """);
            }

            var mealTimeText = !string.IsNullOrEmpty(m.Instruction) ? m.Instruction : "Nutrition Focus";

            mealCards.Append($"""
                <div class="meal-card" style="background: var(--coach-dark-elevated); border: 1px solid var(--coach-gray-dark); border-radius: 4px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.3); margin-bottom: 16px;">
                  <div class="meal-header" style="display: flex; justify-content: space-between; align-items: center; padding: 10px 16px; background: var(--coach-black); border-bottom: 2px solid var(--coach-primary);">
                    <div class="meal-title" style="font-family: var(--coach-font-body); font-size: 13px; font-weight: 800; letter-spacing: 1px; text-transform: uppercase; color: var(--coach-white);">{icon} {m.Name}</div>
                    <div class="meal-time" style="font-size: 9px; font-weight: 600; color: var(--coach-primary); letter-spacing: 0.5px;">{mealTimeText}</div>
                  </div>
                  <div class="meal-body" style="padding: 12px 16px;">
                    {foodItems}
                  </div>
                </div>
                """);
        }

        var notesSection = "";
        if (!string.IsNullOrEmpty(diet.Instructions))
        {
            notesSection = $"""
                <div class="notes-section" style="margin-top: auto; padding: 12px 16px; background: var(--coach-black); border-left: 3px solid var(--coach-secondary); border-radius: 4px;">
                  <div class="notes-title" style="font-family: var(--coach-font-body); font-size: 11px; font-weight: 800; letter-spacing: 0.5px; text-transform: uppercase; color: var(--coach-secondary); margin-bottom: 6px;">📝 SPECIAL COACHING INSTRUCTIONS & NOTES</div>
                  <div class="notes-text" style="font-size: 10px; line-height: 1.5; color: var(--coach-gray-light);">{diet.Instructions}</div>
                </div>
                """;
        }

        return $"""
        <div class="a4-page">
          <div class="diagonal-overlay"></div>
          <div class="content-wrapper">
            <header class="header">
              <div>
                <h2>DIET & NUTRITION PLAN</h2>
                <p>Precision macronutrient coaching blueprint</p>
              </div>
              <div>
                <span class="skew-badge"><span>NUTRITION</span></span>
              </div>
            </header>

            <!-- Daily Macros Summary -->
            <div class="macro-summary">
              <div class="macro-card">
                <div class="macro-label">CALORIES</div>
                <div class="macro-value">{totalCal}</div>
                <div class="macro-unit">kcal</div>
              </div>
              <div class="macro-card">
                <div class="macro-label">PROTEIN</div>
                <div class="macro-value">{totalProtein}g</div>
                <div class="macro-unit">{pPct}%</div>
              </div>
              <div class="macro-card">
                <div class="macro-label">CARBS</div>
                <div class="macro-value">{totalCarbs}g</div>
                <div class="macro-unit">{cPct}%</div>
              </div>
              <div class="macro-card">
                <div class="macro-label">FATS</div>
                <div class="macro-value">{totalFats}g</div>
                <div class="macro-unit">{fPct}%</div>
              </div>
            </div>

            <!-- Meals Container -->
            <div class="meals-container" style="flex: 1; overflow: hidden; display: flex; flex-direction: column;">
              {mealCards}
            </div>

            {notesSection}
          </div>

          <footer class="footer">
            <div>
              <a href="{instagramLink}" target="_blank" style="color: var(--coach-gray-light); text-decoration: none; display: flex; align-items: center; gap: 6px;">
                <svg style="width: 14px; height: 14px; fill: var(--coach-gray-light);" viewBox="0 0 24 24">
                  <path d="M12 2.163c3.204 0 3.584.012 4.85.07 3.252.148 4.771 1.691 4.919 4.919.058 1.265.069 1.645.069 4.849 0 3.205-.012 3.584-.069 4.849-.149 3.225-1.664 4.771-4.919 4.919-1.266.058-1.644.07-4.85.07-3.204 0-3.584-.012-4.849-.07-3.26-.149-4.771-1.699-4.919-4.92-.058-1.265-.07-1.644-.07-4.849 0-3.204.013-3.583.07-4.849.149-3.227 1.664-4.771 4.919-4.919 1.266-.057 1.645-.069 4.849-.069zm0-2c-3.259 0-3.667.014-4.947.072-4.358.2-6.78 2.618-6.98 6.98-.059 1.281-.073 1.689-.073 4.948 0 3.259.014 3.668.072 4.948.2 4.358 2.618 6.78 6.98 6.98 1.281.058 1.689.072 4.948.072 3.259 0 3.668-.014 4.948-.072 4.354-.2 6.782-2.618 6.979-6.98.059-1.28.073-1.689.073-4.948 0-3.259-.014-3.667-.072-4.947-.196-4.354-2.617-6.78-6.979-6.98-1.281-.059-1.69-.073-4.949-.073zm0 5.838c-3.403 0-6.162 2.759-6.162 6.162s2.759 6.163 6.162 6.163 6.162-2.759 6.162-6.163c0-3.403-2.759-6.162-6.162-6.162zm0 10.162c-2.209 0-4-1.79-4-4 0-2.209 1.791-4 4-4s4 1.791 4 4c0 2.21-1.791 4-4 4zm6.406-11.845c-.796 0-1.441.645-1.441 1.44s.645 1.44 1.441 1.44c.795 0 1.439-.645 1.439-1.44s-.644-1.44-1.439-1.44z"/>
                </svg>
                <span>@{instagram}</span>
              </a>
            </div>
            <div><span>✉</span><span>{email}</span></div>
            <div><span>📞</span><span>{phone}</span></div>
          </footer>
        </div>
        """;
    }

    // ── CSS & STYLING (تم تعديل الخطوط لتصبح مدمجة وسريعة جداً) ──
    private static string SharedCss(string primaryColor, string primaryGlow, string secondaryColor) => $$"""
        * {
          margin: 0;
          padding: 0;
          box-sizing: border-box;
        }

        :root {
          /* Brand Colors */
          --coach-primary: {{primaryColor}};
          --coach-primary-glow: {{primaryGlow}};
          --coach-secondary: {{secondaryColor}};
          --coach-dark: #0A0A0F;
          --coach-dark-elevated: #1A1A24;
          --coach-black: #000000;
          --coach-white: #FFFFFF;
          --coach-gray-light: #8A8A9E;
          --coach-gray-dark: #3A3A4A;

          /* Typography (تم استبدالها بخطوط النظام فائقة السرعة التي تدعم اللغتين بكفاءة) */
          --coach-font-body: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif, "Apple Color Emoji", "Segoe UI Emoji";

          /* Spacing */
          --spacing-xs: 8px;
          --spacing-sm: 12px;
          --spacing-md: 20px;
          --spacing-lg: 32px;
          --spacing-xl: 48px;

          /* Page Dimensions */
          --page-width: 210mm;
          --page-height: 297mm;
          --page-padding: 24px;
        }

        body {
          margin: 0;
          padding: 0;
          background: #e5e5e5;
          font-family: var(--coach-font-body);
          -webkit-print-color-adjust: exact;
          print-color-adjust: exact;
        }

        .a4-page {
          width: var(--page-width);
          height: var(--page-height);
          box-sizing: border-box;
          overflow: hidden;
          position: relative;
          background: var(--coach-dark);
          color: var(--coach-white);
          font-size: 10px;
          line-height: 1.5;
          page-break-after: always;
          page-break-inside: avoid;
          margin: 0 auto;
        }

        .content-wrapper {
          position: relative;
          width: 100%;
          height: 100%;
          padding: var(--page-padding);
          padding-bottom: 84px;
          box-sizing: border-box;
          z-index: 2;
          display: flex;
          flex-direction: column;
        }

        /* GLOBAL FOOTER */
        .footer {
          position: absolute;
          bottom: 0;
          left: 0;
          right: 0;
          height: 60px;
          background: var(--coach-black);
          border-top: 2px solid var(--coach-primary);
          display: flex;
          align-items: center;
          justify-content: space-between;
          padding: 0 var(--page-padding);
          gap: var(--spacing-md);
          font-size: 9px;
          font-weight: 500;
          color: var(--coach-gray-light);
          z-index: 10;
        }

        .footer > div {
          display: flex;
          align-items: center;
          gap: 6px;
        }

        /* BACKGROUND DECORATIVE ELEMENTS */
        .diagonal-overlay {
          position: absolute;
          top: 0;
          right: 0;
          width: 100%;
          height: 100%;
          background: linear-gradient(115deg, transparent 65%, rgba(26, 26, 36, 0.35) 65%);
          z-index: 1;
          pointer-events: none;
        }

        .accent-bar {
          position: absolute;
          height: 4px;
          background: linear-gradient(90deg, var(--coach-primary) 0%, var(--coach-secondary) 100%);
        }

        /* COVER PAGE STYLES */
        .cover-grid {
          display: grid;
          grid-template-columns: 1fr 1.2fr;
          gap: var(--spacing-lg);
          height: calc(100% - 60px);
        }

        .cover-photo-section {
          position: relative;
          display: flex;
          align-items: center;
          justify-content: center;
          overflow: hidden;
          height: 100%;
        }

        .coach-photo {
          width: 100%;
          height: 100%;
          object-fit: cover;
          object-position: 25% center;
          border-radius: 4px;
          border: 1px solid var(--coach-gray-dark);
        }

        .cover-info-section {
          display: flex;
          flex-direction: column;
          justify-content: center;
          gap: 12px;
          padding: var(--spacing-xs);
        }

        .display-massive {
          font-weight: 900;
          text-transform: uppercase;
        }

        .doc-title-label {
          display: block;
          width: 100%;
          padding: 12px 16px;
          background: rgba(255, 0, 102, 0.12);
          border: 2px solid var(--coach-secondary);
          color: var(--coach-secondary);
          font-size: 14px;
          font-weight: 900;
          letter-spacing: 1.5px;
          text-transform: uppercase;
          border-radius: 4px;
          text-align: center;
          box-shadow: 0 0 12px rgba(255, 0, 102, 0.25);
          box-sizing: border-box;
        }

        .client-meta {
          display: flex;
          flex-direction: column;
          gap: var(--spacing-xs);
          padding: 12px var(--spacing-md);
          background: var(--coach-dark-elevated);
          border-left: 3px solid var(--coach-primary);
          border-radius: 4px;
        }

        .client-meta-row {
          display: flex;
          justify-content: space-between;
          font-size: 9px;
        }

        .client-meta-label {
          color: var(--coach-gray-light);
          font-weight: 600;
        }

        .client-meta-value {
          color: var(--coach-white);
          font-weight: 700;
        }

        /* HEADER OF PAGES */
        .header {
          display: flex;
          justify-content: space-between;
          align-items: flex-start;
          margin-bottom: var(--spacing-md);
          padding-bottom: var(--spacing-sm);
          border-bottom: 2px solid var(--coach-gray-dark);
        }

        .header h2 {
          font-size: 26px;
          font-weight: 900;
          letter-spacing: 1px;
          text-transform: uppercase;
          margin: 0 0 2px 0;
          line-height: 1;
        }

        .header p {
          font-size: 10px;
          color: var(--coach-gray-light);
          font-weight: 500;
          letter-spacing: 0.5px;
          margin: 0;
        }

        .skew-badge {
          display: inline-block;
          padding: 4px 12px;
          background: var(--coach-primary);
          color: var(--coach-black);
          font-size: 10px;
          font-weight: 800;
          letter-spacing: 0.5px;
          text-transform: uppercase;
          transform: skewX(-8deg);
          box-shadow: 0 4px 16px rgba(0, 0, 0, 0.4);
        }

        .skew-badge span {
          display: inline-block;
          transform: skewX(8deg);
        }

        /* EXERCISE TABLE STYLES */
        .table-container {
          width: 100%;
          margin-bottom: var(--spacing-sm);
        }

        .table-container table {
          width: 100%;
          border-collapse: separate;
          border-spacing: 0;
          background: var(--coach-dark-elevated);
          border-radius: 4px;
          overflow: hidden;
          box-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
        }

        .table-container thead {
          background: var(--coach-black);
        }

        .table-container th {
          padding: 10px 14px;
          text-align: left;
          font-size: 10px;
          font-weight: 800;
          letter-spacing: 1px;
          text-transform: uppercase;
          color: var(--coach-white);
          border-bottom: 2px solid var(--coach-primary);
        }

        .table-container tbody tr {
          border-bottom: 1px solid var(--coach-gray-dark);
        }

        .table-container tbody tr:last-child tr {
          border-bottom: none;
        }

        .table-container td {
          padding: 10px 14px;
          font-size: 10px;
          font-weight: 500;
          color: var(--coach-white);
          border-bottom: 1px solid rgba(255, 255, 255, 0.05);
        }

        .exercise-name {
          font-size: 10px;
          font-weight: 700;
          letter-spacing: 0.3px;
          color: var(--coach-white);
        }

        /* CARDIO SPECIFICATION STYLES */
        .cardio-section {
          background: var(--coach-dark-elevated);
          border: 1px solid var(--coach-gray-dark);
          border-radius: 4px;
          padding: var(--spacing-md);
          box-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
        }

        .cardio-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: var(--spacing-sm);
          padding-bottom: 6px;
          border-bottom: 1px solid var(--coach-gray-dark);
        }

        .cardio-title {
          font-size: 12px;
          font-weight: 800;
          letter-spacing: 1px;
          text-transform: uppercase;
          color: var(--coach-white);
        }

        .cardio-badge {
          padding: 3px 10px;
          background: var(--coach-secondary);
          color: var(--coach-white);
          font-size: 8px;
          font-weight: 800;
          letter-spacing: 0.5px;
          text-transform: uppercase;
          border-radius: 3px;
        }

        .cardio-grid {
          display: grid;
          grid-template-columns: repeat(3, 1fr);
          gap: var(--spacing-md);
        }

        .cardio-metric {
          display: flex;
          flex-direction: column;
          gap: 4px;
        }

        .cardio-metric-label {
          font-size: 7px;
          font-weight: 700;
          letter-spacing: 0.5px;
          text-transform: uppercase;
          color: var(--coach-gray-light);
        }

        .cardio-metric-val {
          font-size: 13px;
          font-weight: 800;
          letter-spacing: 0.5px;
          color: var(--coach-white);
        }

        /* MACROS SUMMARY STYLES */
        .macro-summary {
          display: grid;
          grid-template-columns: repeat(4, 1fr);
          gap: var(--spacing-sm);
          margin-bottom: var(--spacing-md);
          padding: var(--spacing-sm);
          background: var(--coach-dark-elevated);
          border-radius: 4px;
          border: 1px solid var(--coach-gray-dark);
        }

        .macro-card {
          display: flex;
          flex-direction: column;
          align-items: center;
          gap: 4px;
          padding: var(--spacing-xs);
          background: var(--coach-black);
          border-radius: 4px;
        }

        .macro-label {
          font-size: 7px;
          font-weight: 700;
          letter-spacing: 0.5px;
          text-transform: uppercase;
          color: var(--coach-gray-light);
        }

        .macro-value {
          font-size: 20px;
          font-weight: 900;
          color: var(--coach-primary);
        }

        .macro-unit {
          font-size: 8px;
          color: var(--coach-gray-light);
          font-weight: 600;
        }

        .macro-chip {
          padding: 2px 6px;
          background: var(--coach-black);
          border-radius: 3px;
          white-space: nowrap;
          font-size: 8px;
          font-weight: 600;
        }

        .macro-chip.protein { color: #00D9FF; }
        .macro-chip.carbs { color: #FFD700; }
        .macro-chip.fats { color: #FF6B00; }

        .text-primary {
          color: var(--coach-primary);
        }
        """;

    // ── HELPERS ───────────────────────────────────────────────────
    private string GetCoachPhotoBase64(string? profilePicturePath)
    {
        try
        {
            if (!string.IsNullOrEmpty(profilePicturePath))
            {
                var cleanPath = profilePicturePath.TrimStart('/');
                var fullPath = Path.Combine(_webRootPath, cleanPath);
                if (File.Exists(fullPath))
                {
                    var bytes = File.ReadAllBytes(fullPath);
                    var ext = Path.GetExtension(fullPath).ToLower().TrimStart('.');
                    var mimeType = ext == "png" ? "image/png" : "image/jpeg";
                    return $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";
                }
            }

            var path1 = Path.Combine(Directory.GetCurrentDirectory(), "templates", "imgT.jpeg");
            var path2 = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())?.FullName ?? "", "templates", "imgT.jpeg");
            var fallbackPath = File.Exists(path1) ? path1 : (File.Exists(path2) ? path2 : "");

            if (string.IsNullOrEmpty(fallbackPath))
            {
                var fallbackPathOld1 = Path.Combine(Directory.GetCurrentDirectory(), "templates", "img.jpeg");
                var fallbackPathOld2 = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())?.FullName ?? "", "templates", "img.jpeg");
                fallbackPath = File.Exists(fallbackPathOld1) ? fallbackPathOld1 : (File.Exists(fallbackPathOld2) ? fallbackPathOld2 : "");
            }
            
            if (!string.IsNullOrEmpty(fallbackPath) && File.Exists(fallbackPath))
            {
                var bytes = File.ReadAllBytes(fallbackPath);
                return $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}";
            }
        }
        catch
        {
            // Fail silently
        }

        return "https://images.unsplash.com/photo-1571019614242-c5c5dee9f50b?w=800&q=80";
    }
}