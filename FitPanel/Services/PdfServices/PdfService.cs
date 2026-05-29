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
    private readonly string _contentRootPath;
    private readonly IConfiguration _config;

    public PdfService(FitPanelDbContext db, IWebHostEnvironment env, IConfiguration config)
    {
        _db = db;
        _webRootPath = env.WebRootPath;
        _contentRootPath = env.ContentRootPath;
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

        // Notes page appears as page 2 (after cover) only if coach added notes
        var notesPage = !string.IsNullOrWhiteSpace(diet.Instructions)
            ? BuildDietNotesPage(diet.Instructions, coachEmail, coachPhone, instagram, instagramLink)
            : "";

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
          {notesPage}
          {DietContentPage(diet, client.Name, coachEmail, coachPhone, instagram, instagramLink, totalCal, totalProtein, totalCarbs, totalFats, pPct, cPct, fPct)}
        </body>
        </html>
        """;
    }

    private static string BuildDietNotesPage(string notes, string email, string phone, string instagram, string instagramLink) => $"""
        <div class="a4-page">
          <div class="diagonal-overlay"></div>
          <div class="content-wrapper">
            <header class="header">
              <div>
                <h2>📋 COACHING NOTES</h2>
                <p>Special guidelines &amp; instructions from your coach</p>
              </div>
              <div><span class="skew-badge"><span>NOTES</span></span></div>
            </header>

            <div style="padding: 24px 28px; background: rgba(255,255,255,0.04); border-left: 4px solid var(--coach-primary); border-radius: 4px; margin-top: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.15);">
              <div style="font-size: 20px; line-height: 1.85; color: #f2f2f2; white-space: pre-wrap; font-weight: 500;">{notes}</div>
            </div>
          </div>

          <footer class="footer">
            <div><a href="{instagramLink}" target="_blank" style="color:var(--coach-gray-light);text-decoration:none;display:flex;align-items:center;gap:6px;"><svg style="width:14px;height:14px;fill:var(--coach-gray-light);" viewBox="0 0 24 24"><path d="M12 2.163c3.204 0 3.584.012 4.85.07 3.252.148 4.771 1.691 4.919 4.919.058 1.265.069 1.645.069 4.849 0 3.205-.012 3.584-.069 4.849-.149 3.225-1.664 4.771-4.919 4.919-1.266.058-1.644.07-4.85.07-3.204 0-3.584-.012-4.849-.07-3.26-.149-4.771-1.699-4.919-4.92-.058-1.265-.07-1.644-.07-4.849 0-3.204.013-3.583.07-4.849.149-3.227 1.664-4.771 4.919-4.919 1.266-.057 1.645-.069 4.849-.069zm0-2c-3.259 0-3.667.014-4.947.072-4.358.2-6.78 2.618-6.98 6.98-.059 1.281-.073 1.689-.073 4.948 0 3.259.014 3.668.072 4.948.2 4.358 2.618 6.78 6.98 6.98 1.281.058 1.689.072 4.948.072 3.259 0 3.668-.014 4.948-.072 4.354-.2 6.782-2.618 6.979-6.98.059-1.28.073-1.689.073-4.948 0-3.259-.014-3.667-.072-4.947-.196-4.354-2.617-6.78-6.979-6.98-1.281-.059-1.69-.073-4.949-.073zm0 5.838c-3.403 0-6.162 2.759-6.162 6.162s2.759 6.163 6.162 6.163 6.162-2.759 6.162-6.163c0-3.403-2.759-6.162-6.162-6.162zm0 10.162c-2.209 0-4-1.79-4-4 0-2.209 1.791-4 4-4s4 1.791 4 4c0 2.21-1.791 4-4 4zm6.406-11.845c-.796 0-1.441.645-1.441 1.44s.645 1.44 1.441 1.44c.795 0 1.439-.645 1.439-1.44s-.644-1.44-1.439-1.44z"/></svg><span>@{instagram}</span></a></div>
            <div><a href="mailto:{email}" style="color:var(--coach-gray-light);text-decoration:none;display:flex;align-items:center;gap:6px;"><svg style="width:14px;height:14px;fill:var(--coach-gray-light);" viewBox="0 0 24 24"><path d="M20 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 4l-8 5-8-5V6l8 5 8-5v2z"/></svg><span>{email}</span></a></div>
            <div><a href="tel:{phone}" style="color:var(--coach-gray-light);text-decoration:none;display:flex;align-items:center;gap:6px;"><svg style="width:14px;height:14px;fill:var(--coach-gray-light);" viewBox="0 0 24 24"><path d="M6.62 10.79c1.44 2.83 3.76 5.14 6.59 6.59l2.2-2.2c.27-.27.67-.36 1.02-.24 1.12.37 2.33.57 3.57.57.55 0 1 .45 1 1V20c0 .55-.45 1-1 1-9.39 0-17-7.61-17-17 0-.55.45-1 1-1h3.5c.55 0 1 .45 1 1 0 1.25.2 2.45.57 3.57.11.35.03.74-.25 1.02l-2.2 2.2z"/></svg><span>{phone}</span></a></div>
          </footer>
        </div>
        """;



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
                      {coachName}
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
            <div>
              <a href="mailto:{coachEmail}" style="color: var(--coach-gray-light); text-decoration: none; display: flex; align-items: center; gap: 6px;">
                <svg style="width: 14px; height: 14px; fill: var(--coach-gray-light);" viewBox="0 0 24 24">
                  <path d="M20 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 4l-8 5-8-5V6l8 5 8-5v2z"/>
                </svg>
                <span>{coachEmail}</span>
              </a>
            </div>
            <div>
              <a href="tel:{coachPhone}" style="color: var(--coach-gray-light); text-decoration: none; display: flex; align-items: center; gap: 6px;">
                <svg style="width: 14px; height: 14px; fill: var(--coach-gray-light);" viewBox="0 0 24 24">
                  <path d="M6.62 10.79c1.44 2.83 3.76 5.14 6.59 6.59l2.2-2.2c.27-.27.67-.36 1.02-.24 1.12.37 2.33.57 3.57.57.55 0 1 .45 1 1V20c0 .55-.45 1-1 1-9.39 0-17-7.61-17-17 0-.55.45-1 1-1h3.5c.55 0 1 .45 1 1 0 1.25.2 2.45.57 3.57.11.35.03.74-.25 1.02l-2.2 2.2z"/>
                </svg>
                <span>{coachPhone}</span>
              </a>
            </div>
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
                      {coachName}
                      <svg style="width: 18px; height: 18px; fill: var(--coach-primary);" viewBox="0 0 24 24">
                        <path d="M12 2.163c3.204 0 3.584.012 4.85.07 3.252.148 4.771 1.691 4.919 4.919.058 1.265.069 1.645.069 4.849 0 3.205-.012 3.584-.069 4.849-.149 3.225-1.664 4.771-4.919 4.919-1.266.058-1.644.07-4.85.07-3.204 0-3.584-.012-4.849-.07-3.26-.149-4.771-1.699-4.919-4.92-.058-1.265-.07-1.644-.07-4.849 0-3.204.013-3.583.07-4.849.149-3.227 1.664-4.771 4.919-4.919 1.266-.057 1.645-.069 4.849-.069zm0-2c-3.259 0-3.667.014-4.947.072-4.358.2-6.78 2.618-6.98 6.98-.059 1.281-.073 1.689-.073 4.948 0 3.259.014 3.668.072 4.948.2 4.358 2.618 6.78 6.98 6.98 1.281.058 1.689.072 4.948.072 3.259 0 3.668-.014 4.948-.072 4.354-.2 6.782-2.618 6.979-6.98.059-1.28.073-1.689.073-4.948 0-3.259-.014-3.667-.072-4.947-.196-4.354-2.617-6.78-6.979-6.98-1.281-.059-1.69-.073-4.949-.073zm0 5.838c-3.403 0-6.162 2.759-6.162 6.162s2.759 6.163 6.162 6.163 6.162-2.759 6.162-6.163c0-3.403-2.759-6.162-6.162-6.162zm0 10.162c-2.209 0-4-1.79-4-4 0-2.209 1.791-4 4-4s4 1.791 4 4c0 2.21-1.791 4-4 4zm6.406-11.845c-.796 0-1.441.645-1.441 1.44s.645 1.44 1.441 1.44c.795 0 1.439-.645 1.439-1.44s-.644-1.44-1.439-1.44z"/>
                      </svg>
                    </a>
                  </p>
                </div>

                <div class="cover-keys-list" style="margin-bottom: 28px;">
                  <div style="font-family: var(--coach-font-body); font-size: 15px; font-weight: 800; color: var(--coach-primary); letter-spacing: 1px; text-transform: uppercase; margin-bottom: 10px;">OUR KEYS TO SUCCESS</div>
                  <ul style="list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 6px; font-size: 12px; font-weight: 500; color: var(--coach-gray-light);">
                    <li style="display: flex; align-items: center; gap: 8px;"><span style="color: var(--coach-primary);">⚡</span> WORKOUT</li>
                    <li style="display: flex; align-items: center; gap: 8px;"><span style="color: var(--coach-primary);">⚡</span> ABS</li>
                    <li style="display: flex; align-items: center; gap: 8px;"><span style="color: var(--coach-primary);">⚡</span> NUTRITION</li>
                    <li style="display: flex; align-items: center; gap: 8px;"><span style="color: var(--coach-primary);">⚡</span> CARDIO</li>
                    <li style="display: flex; align-items: center; gap: 8px;"><span style="color: var(--coach-primary);">⚡</span> SUPPLEMENTATION</li>
                  </ul>
                  <div style="margin-top: 14px; font-style: italic; font-size: 14px; color: var(--coach-white); font-weight: 600;">
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
            <div>
              <a href="mailto:{coachEmail}" style="color: var(--coach-gray-light); text-decoration: none; display: flex; align-items: center; gap: 6px;">
                <svg style="width: 14px; height: 14px; fill: var(--coach-gray-light);" viewBox="0 0 24 24">
                  <path d="M20 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 4l-8 5-8-5V6l8 5 8-5v2z"/>
                </svg>
                <span>{coachEmail}</span>
              </a>
            </div>
            <div>
              <a href="tel:{coachPhone}" style="color: var(--coach-gray-light); text-decoration: none; display: flex; align-items: center; gap: 6px;">
                <svg style="width: 14px; height: 14px; fill: var(--coach-gray-light);" viewBox="0 0 24 24">
                  <path d="M6.62 10.79c1.44 2.83 3.76 5.14 6.59 6.59l2.2-2.2c.27-.27.67-.36 1.02-.24 1.12.37 2.33.57 3.57.57.55 0 1 .45 1 1V20c0 .55-.45 1-1 1-9.39 0-17-7.61-17-17 0-.55.45-1 1-1h3.5c.55 0 1 .45 1 1 0 1.25.2 2.45.57 3.57.11.35.03.74-.25 1.02l-2.2 2.2z"/>
                </svg>
                <span>{coachPhone}</span>
              </a>
            </div>
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
                  <td style="text-align: center; font-weight: 800; font-size: 15px;">{ex.Sets}</td>
                  <td style="text-align: center; font-weight: 700; font-size: 15px;">{ex.Reps}</td>
                  <td style="text-align: center; color: var(--coach-primary); font-weight: 800; font-size: 15px;">{ex.RestTime}s</td>
                </tr>
                """);
        }

        var cardioSection = "";
        if (day.Cardio != null)
        {
            var c = day.Cardio;
            cardioSection = $"""
                <div class="cardio-section" style="margin-top: 16px;">
                  <div class="cardio-header">
                    <div class="cardio-title">🏃 CARDIO PROTOCOL</div>
                    <div class="cardio-badge">{c.Intensity}</div>
                  </div>
                  <div class="cardio-grid">
                    <div class="cardio-metric">
                      <div class="cardio-metric-label">TYPE / METHOD</div>
                      <div class="cardio-metric-val">{c.CardioType}</div>
                    </div>
                    <div class="cardio-metric">
                      <div class="cardio-metric-label">DURATION</div>
                      <div class="cardio-metric-val text-primary">{c.DurationMinutes} min</div>
                    </div>
                    <div class="cardio-metric">
                      <div class="cardio-metric-label">NOTES</div>
                      <div class="cardio-metric-val">{c.Notes ?? "—"}</div>
                    </div>
                  </div>
                </div>
                """;
        }

        var dayNotesSection = "";
        if (!string.IsNullOrWhiteSpace(day.Notes))
        {
            dayNotesSection = $"""
                <div style="margin-top: 16px; padding: 16px 20px; background: rgba(255,255,255,0.04); border-left: 4px solid var(--coach-primary); border-radius: 4px; box-shadow: 0 2px 8px rgba(0,0,0,0.2);">
                  <div style="font-size: 11px; font-weight: 900; letter-spacing: 1.2px; text-transform: uppercase; color: var(--coach-primary); margin-bottom: 8px;">📋 DAY NOTES &amp; INSTRUCTIONS</div>
                  <div style="font-size: 18px; line-height: 1.7; color: #f0f0f0; font-weight: 500; white-space: pre-wrap;">{day.Notes}</div>
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
                    <th style="width: 50%;">MOVEMENT &amp; PATTERN</th>
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

            {dayNotesSection}
            {cardioSection}
          </div>

          <footer class="footer">
            <div>
              <a href="{instagramLink}" target="_blank" style="color: var(--coach-gray-light); text-decoration: none; display: flex; align-items: center; gap: 6px;">
                <svg style="width: 14px; height: 14px; fill: var(--coach-gray-light);" viewBox="0 0 24 24"><path d="M12 2.163c3.204 0 3.584.012 4.85.07 3.252.148 4.771 1.691 4.919 4.919.058 1.265.069 1.645.069 4.849 0 3.205-.012 3.584-.069 4.849-.149 3.225-1.664 4.771-4.919 4.919-1.266.058-1.644.07-4.85.07-3.204 0-3.584-.012-4.849-.07-3.26-.149-4.771-1.699-4.919-4.92-.058-1.265-.07-1.644-.07-4.849 0-3.204.013-3.583.07-4.849.149-3.227 1.664-4.771 4.919-4.919 1.266-.057 1.645-.069 4.849-.069zm0-2c-3.259 0-3.667.014-4.947.072-4.358.2-6.78 2.618-6.98 6.98-.059 1.281-.073 1.689-.073 4.948 0 3.259.014 3.668.072 4.948.2 4.358 2.618 6.78 6.98 6.98 1.281.058 1.689.072 4.948.072 3.259 0 3.668-.014 4.948-.072 4.354-.2 6.782-2.618 6.979-6.98.059-1.28.073-1.689.073-4.948 0-3.259-.014-3.667-.072-4.947-.196-4.354-2.617-6.78-6.979-6.98-1.281-.059-1.69-.073-4.949-.073zm0 5.838c-3.403 0-6.162 2.759-6.162 6.162s2.759 6.163 6.162 6.163 6.162-2.759 6.162-6.163c0-3.403-2.759-6.162-6.162-6.162zm0 10.162c-2.209 0-4-1.79-4-4 0-2.209 1.791-4 4-4s4 1.791 4 4c0 2.21-1.791 4-4 4zm6.406-11.845c-.796 0-1.441.645-1.441 1.44s.645 1.44 1.441 1.44c.795 0 1.439-.645 1.439-1.44s-.644-1.44-1.439-1.44z"/></svg>
                <span>@{instagram}</span>
              </a>
            </div>
            <div>
              <a href="mailto:{email}" style="color: var(--coach-gray-light); text-decoration: none; display: flex; align-items: center; gap: 6px;">
                <svg style="width: 14px; height: 14px; fill: var(--coach-gray-light);" viewBox="0 0 24 24"><path d="M20 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 4l-8 5-8-5V6l8 5 8-5v2z"/></svg>
                <span>{email}</span>
              </a>
            </div>
            <div>
              <a href="tel:{phone}" style="color: var(--coach-gray-light); text-decoration: none; display: flex; align-items: center; gap: 6px;">
                <svg style="width: 14px; height: 14px; fill: var(--coach-gray-light);" viewBox="0 0 24 24"><path d="M6.62 10.79c1.44 2.83 3.76 5.14 6.59 6.59l2.2-2.2c.27-.27.67-.36 1.02-.24 1.12.37 2.33.57 3.57.57.55 0 1 .45 1 1V20c0 .55-.45 1-1 1-9.39 0-17-7.61-17-17 0-.55.45-1 1-1h3.5c.55 0 1 .45 1 1 0 1.25.2 2.45.57 3.57.11.35.03.74-.25 1.02l-2.2 2.2z"/></svg>
                <span>{phone}</span>
              </a>
            </div>
          </footer>
        </div>
        """;
    }

    // ── DIET PLAN PAGE ────────────────────────────────────────────
    // Each DietMeal gets its own A4 page. The first page has the macro summary.
    private static string DietContentPage(
        Diet diet, string clientName, string email, string phone, string instagram, string instagramLink,
        int totalCal, int totalProtein, int totalCarbs, int totalFats,
        int pPct, int cPct, int fPct)
    {
        var pagesSb = new StringBuilder();
        bool isFirst = true;

        // Only count top-level meals (not alternative whole meals which have a ParentDietMealId)
        var topLevelMeals = diet.DietMeals
            .Where(m => m.ParentDietMealId == null)
            .OrderBy(m => m.Id)
            .ToList();
        int totalMealCount = topLevelMeals.Count;
        int mealNumber = 0;

        foreach (var m in topLevelMeals)
        {
            mealNumber++;
            string icon = "🍽️";
            var mealNameLower = m.Name.ToLower();
            if (mealNameLower.Contains("breakfast") || mealNameLower.Contains("morning")) icon = "🍳";
            else if (mealNameLower.Contains("lunch") || mealNameLower.Contains("dinner") || mealNameLower.Contains("night")) icon = "🍗";
            else if (mealNameLower.Contains("snack") || mealNameLower.Contains("shake") || mealNameLower.Contains("smoothie")) icon = "🥤";
            else if (mealNameLower.Contains("pre") || mealNameLower.Contains("intra") || mealNameLower.Contains("workout")) icon = "⚡";

            var foodItems = new StringBuilder();
            foreach (var item in m.MealItems)
            {
                var portionText = $"{item.Quantity} {item.Unit}";
                var altHtml = new StringBuilder();
                if (item.AlternativeItems != null && item.AlternativeItems.Any())
                {
                    altHtml.Append("<div style='margin-top:6px; padding-left:14px; border-left:2px solid var(--coach-secondary);'>");
                    foreach (var alt in item.AlternativeItems)
                        altHtml.Append($"<div style='font-size:11px; color:var(--coach-gray-light); padding:3px 0;'>↳ <strong>{alt.MealName}</strong> — {alt.Quantity} {alt.Unit} &nbsp;<span style='color:#00D9FF;'>P:{alt.Protein}g</span> <span style='color:#FFD700;'>C:{alt.Carbs}g</span> <span style='color:#FF6B00;'>F:{alt.Fats}g</span></div>");
                    altHtml.Append("</div>");
                }

                foodItems.Append($"""
                    <div style="padding: 12px 0; border-bottom: 1px solid var(--coach-gray-dark);">
                      <div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 6px;">
                        <div style="font-size: 14px; font-weight: 700; color: var(--coach-white); flex: 1;">{item.MealName}</div>
                        <div style="font-size: 13px; color: var(--coach-gray-light); padding-right: 16px;">{portionText}</div>
                        <div style="display: flex; gap: 8px;">
                          <span style="padding: 3px 8px; background: var(--coach-black); border-radius: 3px; font-size: 12px; font-weight: 600; color: #00D9FF;">P: {item.Protein}g</span>
                          <span style="padding: 3px 8px; background: var(--coach-black); border-radius: 3px; font-size: 12px; font-weight: 600; color: #FFD700;">C: {item.Carbs}g</span>
                          <span style="padding: 3px 8px; background: var(--coach-black); border-radius: 3px; font-size: 12px; font-weight: 600; color: #FF6B00;">F: {item.Fats}g</span>
                        </div>
                      </div>
                      {altHtml}
                    </div>
                    """);
            }

            // Instruction / notes for this meal
            var instrHtml = "";
            if (!string.IsNullOrWhiteSpace(m.Instruction))
            {
                instrHtml = $"""
                    <div style="margin-top: 20px; padding: 16px 20px; background: rgba(255,255,255,0.04); border-left: 4px solid var(--coach-secondary); border-radius: 4px; box-shadow: 0 2px 8px rgba(0,0,0,0.15);">
                      <div style="font-size: 11px; font-weight: 900; letter-spacing: 1.2px; text-transform: uppercase; color: var(--coach-secondary); margin-bottom: 8px;">📝 MEAL INSTRUCTIONS</div>
                      <div style="font-size: 15px; line-height: 1.8; color: #f0f0f0; font-weight: 500; white-space: pre-wrap;">{m.Instruction}</div>
                    </div>
                    """;
            }

            // Meal name: hyperlink if Link exists in DB
            var mealNameHtml = string.IsNullOrEmpty(m.Link)
                ? m.Name
                : $"<a href='{m.Link}' style='color:var(--coach-primary);text-decoration:none;font-weight:900;display:inline-flex;align-items:center;gap:6px;'>{m.Name} <i style='font-size:11px;'>🔗</i></a>";

            var mealCalTotal = m.MealItems.Sum(i => i.Calories);
            var mealProtTotal = m.MealItems.Sum(i => i.Protein);
            var mealCarbTotal = m.MealItems.Sum(i => i.Carbs);
            var mealFatTotal = m.MealItems.Sum(i => i.Fats);

            pagesSb.Append($"""
            <div class="a4-page">
              <div class="diagonal-overlay"></div>
              <div class="content-wrapper">
                <header class="header">
                  <div>
                    <h2>{icon} <span class="text-primary" style="font-size:0.85em; font-weight:900;">MEAL {mealNumber}</span> — {mealNameHtml}</h2>
                    <p>Personalized diet &amp; nutrition plan &nbsp;·&nbsp; {mealNumber} of {totalMealCount}</p>
                  </div>
                  <div>
                    <span class="skew-badge"><span>MEAL {mealNumber}/{totalMealCount}</span></span>
                  </div>
                </header>


                <div style="background: var(--coach-dark-elevated); border: 1px solid var(--coach-gray-dark); border-radius: 4px; overflow: hidden; padding: 0 16px;">
                  <div style="display:flex; justify-content:space-between; align-items:center; padding: 12px 0; border-bottom: 2px solid var(--coach-primary); margin-bottom: 4px;">
                    <div style="font-size:15px; font-weight:800; letter-spacing:1px; text-transform:uppercase; color:var(--coach-white);">Ingredients</div>
                    <div style="display:flex; gap:10px; font-size:12px; font-weight:700;">
                      <span style="color:#00D9FF;">P: {mealProtTotal}g</span>
                      <span style="color:#FFD700;">C: {mealCarbTotal}g</span>
                      <span style="color:#FF6B00;">F: {mealFatTotal}g</span>
                      <span style="color:var(--coach-primary);">{mealCalTotal} kcal</span>
                    </div>
                  </div>
                  {foodItems}
                </div>

                {instrHtml}
              </div>

              <footer class="footer">
                <div><a href="{instagramLink}" target="_blank" style="color:var(--coach-gray-light);text-decoration:none;display:flex;align-items:center;gap:6px;"><svg style="width:14px;height:14px;fill:var(--coach-gray-light);" viewBox="0 0 24 24"><path d="M12 2.163c3.204 0 3.584.012 4.85.07 3.252.148 4.771 1.691 4.919 4.919.058 1.265.069 1.645.069 4.849 0 3.205-.012 3.584-.069 4.849-.149 3.225-1.664 4.771-4.919 4.919-1.266.058-1.644.07-4.85.07-3.204 0-3.584-.012-4.849-.07-3.26-.149-4.771-1.699-4.919-4.92-.058-1.265-.07-1.644-.07-4.849 0-3.204.013-3.583.07-4.849.149-3.227 1.664-4.771 4.919-4.919 1.266-.057 1.645-.069 4.849-.069zm0-2c-3.259 0-3.667.014-4.947.072-4.358.2-6.78 2.618-6.98 6.98-.059 1.281-.073 1.689-.073 4.948 0 3.259.014 3.668.072 4.948.2 4.358 2.618 6.78 6.98 6.98 1.281.058 1.689.072 4.948.072 3.259 0 3.668-.014 4.948-.072 4.354-.2 6.782-2.618 6.979-6.98.059-1.28.073-1.689.073-4.948 0-3.259-.014-3.667-.072-4.947-.196-4.354-2.617-6.78-6.979-6.98-1.281-.059-1.69-.073-4.949-.073zm0 5.838c-3.403 0-6.162 2.759-6.162 6.162s2.759 6.163 6.162 6.163 6.162-2.759 6.162-6.163c0-3.403-2.759-6.162-6.162-6.162zm0 10.162c-2.209 0-4-1.79-4-4 0-2.209 1.791-4 4-4s4 1.791 4 4c0 2.21-1.791 4-4 4zm6.406-11.845c-.796 0-1.441.645-1.441 1.44s.645 1.44 1.441 1.44c.795 0 1.439-.645 1.439-1.44s-.644-1.44-1.439-1.44z"/></svg><span>@{instagram}</span></a></div>
                <div><a href="mailto:{email}" style="color:var(--coach-gray-light);text-decoration:none;display:flex;align-items:center;gap:6px;"><svg style="width:14px;height:14px;fill:var(--coach-gray-light);" viewBox="0 0 24 24"><path d="M20 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 4l-8 5-8-5V6l8 5 8-5v2z"/></svg><span>{email}</span></a></div>
                <div><a href="tel:{phone}" style="color:var(--coach-gray-light);text-decoration:none;display:flex;align-items:center;gap:6px;"><svg style="width:14px;height:14px;fill:var(--coach-gray-light);" viewBox="0 0 24 24"><path d="M6.62 10.79c1.44 2.83 3.76 5.14 6.59 6.59l2.2-2.2c.27-.27.67-.36 1.02-.24 1.12.37 2.33.57 3.57.57.55 0 1 .45 1 1V20c0 .55-.45 1-1 1-9.39 0-17-7.61-17-17 0-.55.45-1 1-1h3.5c.55 0 1 .45 1 1 0 1.25.2 2.45.57 3.57.11.35.03.74-.25 1.02l-2.2 2.2z"/></svg><span>{phone}</span></a></div>
              </footer>
            </div>
            """);
        }

        // ── SUMMARY LAST PAGE ─────────────────────────────────────
        var mealRows = new StringBuilder();
        int rowNum = 0;
        foreach (var m in topLevelMeals)
        {
            rowNum++;
            var mCal  = m.MealItems.Sum(i => i.Calories);
            var mProt = m.MealItems.Sum(i => i.Protein);
            var mCarb = m.MealItems.Sum(i => i.Carbs);
            var mFat  = m.MealItems.Sum(i => i.Fats);
            var rowBg = rowNum % 2 == 0 ? "background:rgba(255,255,255,0.03);" : "";
            mealRows.Append($"""
                <tr style="{rowBg}">
                  <td style="padding:10px 14px;font-size:13px;font-weight:700;color:var(--coach-white);">
                    <span style="color:var(--coach-primary);font-weight:900;margin-right:8px;">#{rowNum}</span>{m.Name}
                  </td>
                  <td style="padding:10px 14px;text-align:center;font-size:13px;font-weight:700;color:var(--coach-primary);">{mCal}</td>
                  <td style="padding:10px 14px;text-align:center;font-size:13px;font-weight:700;color:#00D9FF;">{mProt}g</td>
                  <td style="padding:10px 14px;text-align:center;font-size:13px;font-weight:700;color:#FFD700;">{mCarb}g</td>
                  <td style="padding:10px 14px;text-align:center;font-size:13px;font-weight:700;color:#FF6B00;">{mFat}g</td>
                </tr>
                """);
        }

        pagesSb.Append($"""
        <div class="a4-page">
          <div class="diagonal-overlay"></div>
          <div class="content-wrapper">
            <header class="header">
              <div>
                <h2>DAILY NUTRITION SUMMARY</h2>
                <p>Total macros across all {totalMealCount} meals</p>
              </div>
              <div><span class="skew-badge"><span>SUMMARY</span></span></div>
            </header>

            <div style="display:grid;grid-template-columns:repeat(4,1fr);gap:12px;margin-bottom:24px;">
              <div style="display:flex;flex-direction:column;align-items:center;gap:4px;padding:16px 8px;background:var(--coach-dark-elevated);border:1px solid var(--coach-primary);border-radius:6px;">
                <div style="font-size:10px;font-weight:700;letter-spacing:1px;text-transform:uppercase;color:var(--coach-gray-light);">CALORIES</div>
                <div style="font-size:36px;font-weight:900;color:var(--coach-primary);">{totalCal}</div>
                <div style="font-size:11px;color:var(--coach-gray-light);font-weight:600;">kcal / day</div>
              </div>
              <div style="display:flex;flex-direction:column;align-items:center;gap:4px;padding:16px 8px;background:var(--coach-dark-elevated);border:1px solid #00D9FF;border-radius:6px;">
                <div style="font-size:10px;font-weight:700;letter-spacing:1px;text-transform:uppercase;color:var(--coach-gray-light);">PROTEIN</div>
                <div style="font-size:36px;font-weight:900;color:#00D9FF;">{totalProtein}g</div>
                <div style="font-size:11px;color:var(--coach-gray-light);font-weight:600;">{pPct}% of macros</div>
              </div>
              <div style="display:flex;flex-direction:column;align-items:center;gap:4px;padding:16px 8px;background:var(--coach-dark-elevated);border:1px solid #FFD700;border-radius:6px;">
                <div style="font-size:10px;font-weight:700;letter-spacing:1px;text-transform:uppercase;color:var(--coach-gray-light);">CARBS</div>
                <div style="font-size:36px;font-weight:900;color:#FFD700;">{totalCarbs}g</div>
                <div style="font-size:11px;color:var(--coach-gray-light);font-weight:600;">{cPct}% of macros</div>
              </div>
              <div style="display:flex;flex-direction:column;align-items:center;gap:4px;padding:16px 8px;background:var(--coach-dark-elevated);border:1px solid #FF6B00;border-radius:6px;">
                <div style="font-size:10px;font-weight:700;letter-spacing:1px;text-transform:uppercase;color:var(--coach-gray-light);">FATS</div>
                <div style="font-size:36px;font-weight:900;color:#FF6B00;">{totalFats}g</div>
                <div style="font-size:11px;color:var(--coach-gray-light);font-weight:600;">{fPct}% of macros</div>
              </div>
            </div>

            <div style="background:var(--coach-dark-elevated);border:1px solid var(--coach-gray-dark);border-radius:6px;overflow:hidden;">
              <table style="width:100%;border-collapse:collapse;">
                <thead>
                  <tr style="background:var(--coach-black);">
                    <th style="padding:10px 14px;text-align:left;font-size:12px;font-weight:900;letter-spacing:1px;text-transform:uppercase;color:var(--coach-white);border-bottom:2px solid var(--coach-primary);">MEAL</th>
                    <th style="padding:10px 14px;text-align:center;font-size:12px;font-weight:900;letter-spacing:1px;text-transform:uppercase;color:var(--coach-primary);border-bottom:2px solid var(--coach-primary);">KCAL</th>
                    <th style="padding:10px 14px;text-align:center;font-size:12px;font-weight:900;letter-spacing:1px;text-transform:uppercase;color:#00D9FF;border-bottom:2px solid var(--coach-primary);">PROTEIN</th>
                    <th style="padding:10px 14px;text-align:center;font-size:12px;font-weight:900;letter-spacing:1px;text-transform:uppercase;color:#FFD700;border-bottom:2px solid var(--coach-primary);">CARBS</th>
                    <th style="padding:10px 14px;text-align:center;font-size:12px;font-weight:900;letter-spacing:1px;text-transform:uppercase;color:#FF6B00;border-bottom:2px solid var(--coach-primary);">FATS</th>
                  </tr>
                </thead>
                <tbody>
                  {mealRows}
                  <tr style="background:var(--coach-black);border-top:2px solid var(--coach-primary);">
                    <td style="padding:12px 14px;font-size:13px;font-weight:900;color:var(--coach-white);">TOTAL</td>
                    <td style="padding:12px 14px;text-align:center;font-size:14px;font-weight:900;color:var(--coach-primary);">{totalCal}</td>
                    <td style="padding:12px 14px;text-align:center;font-size:14px;font-weight:900;color:#00D9FF;">{totalProtein}g</td>
                    <td style="padding:12px 14px;text-align:center;font-size:14px;font-weight:900;color:#FFD700;">{totalCarbs}g</td>
                    <td style="padding:12px 14px;text-align:center;font-size:14px;font-weight:900;color:#FF6B00;">{totalFats}g</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <footer class="footer">
            <div><a href="{instagramLink}" target="_blank" style="color:var(--coach-gray-light);text-decoration:none;display:flex;align-items:center;gap:6px;"><svg style="width:14px;height:14px;fill:var(--coach-gray-light);" viewBox="0 0 24 24"><path d="M12 2.163c3.204 0 3.584.012 4.85.07 3.252.148 4.771 1.691 4.919 4.919.058 1.265.069 1.645.069 4.849 0 3.205-.012 3.584-.069 4.849-.149 3.225-1.664 4.771-4.919 4.919-1.266.058-1.644.07-4.85.07-3.204 0-3.584-.012-4.849-.07-3.26-.149-4.771-1.699-4.919-4.92-.058-1.265-.07-1.644-.07-4.849 0-3.204.013-3.583.07-4.849.149-3.227 1.664-4.771 4.919-4.919 1.266-.057 1.645-.069 4.849-.069zm0-2c-3.259 0-3.667.014-4.947.072-4.358.2-6.78 2.618-6.98 6.98-.059 1.281-.073 1.689-.073 4.948 0 3.259.014 3.668.072 4.948.2 4.358 2.618 6.78 6.98 6.98 1.281.058 1.689.072 4.948.072 3.259 0 3.668-.014 4.948-.072 4.354-.2 6.782-2.618 6.979-6.98.059-1.28.073-1.689.073-4.948 0-3.259-.014-3.667-.072-4.947-.196-4.354-2.617-6.78-6.979-6.98-1.281-.059-1.69-.073-4.949-.073zm0 5.838c-3.403 0-6.162 2.759-6.162 6.162s2.759 6.163 6.162 6.163 6.162-2.759 6.162-6.163c0-3.403-2.759-6.162-6.162-6.162zm0 10.162c-2.209 0-4-1.79-4-4 0-2.209 1.791-4 4-4s4 1.791 4 4c0 2.21-1.791 4-4 4zm6.406-11.845c-.796 0-1.441.645-1.441 1.44s.645 1.44 1.441 1.44c.795 0 1.439-.645 1.439-1.44s-.644-1.44-1.439-1.44z"/></svg><span>@{instagram}</span></a></div>
            <div><a href="mailto:{email}" style="color:var(--coach-gray-light);text-decoration:none;display:flex;align-items:center;gap:6px;"><svg style="width:14px;height:14px;fill:var(--coach-gray-light);" viewBox="0 0 24 24"><path d="M20 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 4l-8 5-8-5V6l8 5 8-5v2z"/></svg><span>{email}</span></a></div>
            <div><a href="tel:{phone}" style="color:var(--coach-gray-light);text-decoration:none;display:flex;align-items:center;gap:6px;"><svg style="width:14px;height:14px;fill:var(--coach-gray-light);" viewBox="0 0 24 24"><path d="M6.62 10.79c1.44 2.83 3.76 5.14 6.59 6.59l2.2-2.2c.27-.27.67-.36 1.02-.24 1.12.37 2.33.57 3.57.57.55 0 1 .45 1 1V20c0 .55-.45 1-1 1-9.39 0-17-7.61-17-17 0-.55.45-1 1-1h3.5c.55 0 1 .45 1 1 0 1.25.2 2.45.57 3.57.11.35.03.74-.25 1.02l-2.2 2.2z"/></svg><span>{phone}</span></a></div>
          </footer>
        </div>
        """);

        return pagesSb.ToString();
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
          font-size: 14px;
          font-weight: 600;
          color: var(--coach-gray-light);
          z-index: 10;
        }

        .footer > div {
          display: flex;
          align-items: center;
          gap: 8px;
        }

        .footer a {
          font-size: 14px !important;
          color: var(--coach-gray-light) !important;
          text-decoration: none !important;
          display: flex !important;
          align-items: center !important;
          gap: 8px !important;
        }

        .footer svg {
          width: 18px !important;
          height: 18px !important;
          fill: var(--coach-gray-light) !important;
        }

        /* BACKGROUND DECORATIVE ELEMENTS */
        .diagonal-overlay {
          position: absolute;
          top: 0;
          right: 0;
          width: 100%;
          height: 100%;
          background: transparent;
          z-index: 1;
          pointer-events: none;
        }

        .accent-bar {
          position: absolute;
          height: 4px;
          background: var(--coach-primary);
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
          font-size: 13px;
        }

        .client-meta-label {
          font-size: 13px;

          color: var(--coach-gray-light);
          font-weight: 700;
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
        }

        .table-container thead {
          background: var(--coach-black);
        }

        .table-container th {
          padding: 12px 16px;
          text-align: left;
          font-size: 13px;
          font-weight: 900;
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
          padding: 12px 16px;
          font-size: 13px;
          font-weight: 500;
          color: var(--coach-white);
          border-bottom: 1px solid rgba(255, 255, 255, 0.05);
        }

        .exercise-name {
          font-size: 13px;
          font-weight: 800;
          letter-spacing: 0.3px;
          color: var(--coach-white);
        }

        /* CARDIO SPECIFICATION STYLES */
        .cardio-section {
          background: var(--coach-dark-elevated);
          border: 1px solid var(--coach-gray-dark);
          border-radius: 4px;
          padding: var(--spacing-md);
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
            // 1. Coach has a custom profile picture stored in wwwroot
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

            // 2. Load fallback image from embedded assembly resource (always works on any server)
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceNames = new[] { "FitPanel.templates.imgT.jpeg", "FitPanel.templates.img.jpeg" };
            foreach (var resourceName in resourceNames)
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    return $"data:image/jpeg;base64,{Convert.ToBase64String(ms.ToArray())}";
                }
            }

            // 3. Last resort: try file system paths (for local dev fallback)
            var possiblePaths = new[]
            {
                Path.Combine(_contentRootPath, "templates", "imgT.jpeg"),
                Path.Combine(AppContext.BaseDirectory, "templates", "imgT.jpeg"),
                Path.Combine(Directory.GetCurrentDirectory(), "templates", "imgT.jpeg"),
            };
            var fallbackPath = possiblePaths.FirstOrDefault(p => !string.IsNullOrEmpty(p) && File.Exists(p));
            if (!string.IsNullOrEmpty(fallbackPath))
            {
                var bytes = File.ReadAllBytes(fallbackPath);
                return $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}";
            }
        }
        catch
        {
            // Fail silently
        }

        // 4. Absolute last resort: external URL
        return "https://images.unsplash.com/photo-1571019614242-c5c5dee9f50b?w=800&q=80";
    }
}