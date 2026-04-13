using FitPanel.Data;
using FitPanel.Data.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace FitPanel.Services;

public class PdfService : IPdfService
{
    private readonly FitPanelDbContext _db;
    private readonly string _webRootPath;

    public PdfService(FitPanelDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _webRootPath = env.WebRootPath;
    }

    // ── IMAGE HELPER ──────────────────────────────────────────────
    private string ImageToBase64(string fileName)
    {
        var fullPath = Path.Combine(_webRootPath, "pdf-assets", fileName);
        if (!File.Exists(fullPath)) return "";
        var bytes = File.ReadAllBytes(fullPath);
        var ext   = Path.GetExtension(fullPath).TrimStart('.').ToLower();
        var mime  = ext == "png" ? "image/png" : "image/jpeg";
        return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
    }

    // ── PUBLIC METHODS ────────────────────────────────────────────
    public async Task<byte[]?> GenerateWorkoutPdfAsync(
        int clientId, int workoutId, string coachId)
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

    public async Task<byte[]?> GenerateDietPdfAsync(
        int clientId, int dietId, string coachId)
    {
        var diet = await _db.Diets
            .Include(d => d.Client)
                .ThenInclude(c => c.Coach)
            .Include(d => d.MealItems)
                .ThenInclude(m => m.AlternativeItems)
            .FirstOrDefaultAsync(d =>
                d.Id == dietId &&
                d.ClientId == clientId &&
                d.Client.CoachId == coachId);

        if (diet == null) return null;
        var html = BuildDietHtml(diet);
        return await RenderToPdfAsync(html);
    }

    // ── PLAYWRIGHT RENDERER ───────────────────────────────────────
    private static async Task<byte[]> RenderToPdfAsync(string html)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync(html, new PageSetContentOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        return await page.PdfAsync(new PagePdfOptions
        {
            Format          = "A4",
            PrintBackground = true,
            Margin          = new Margin { Top = "0", Bottom = "0", Left = "0", Right = "0" }
        });
    }

    // ── WORKOUT HTML BUILDER ──────────────────────────────────────
    private string BuildWorkoutHtml(WorkOut workout)
    {
        var client     = workout.Client;
        var coach      = client.Coach;
        var coachName  = coach?.FullName ?? "الكوتش";
        var coachEmail = coach?.Email ?? "";
        var coachPhone = coach?.PhoneNumber ?? "";
        var days       = workout.WorkOutDays.OrderBy(d => d.Id).ToList();
        int totalPages = days.Count;

        var imgLeft  = ImageToBase64("img-left.jpeg");
        var imgRight = ImageToBase64("img-right.jpeg");

        var pages = new System.Text.StringBuilder();
        for (int i = 0; i < days.Count; i++)
            pages.Append(WorkoutDayPage(days[i], client.Name, coachEmail, coachPhone, i + 1, totalPages));

        return $"""
        <!DOCTYPE html>
        <html lang="ar" dir="rtl">
        <head>
          <meta charset="UTF-8">
          <link href="https://fonts.bunny.net/css?family=barlow+condensed:700,900&display=swap" rel="stylesheet">
          <link href="https://fonts.bunny.net/css?family=barlow:400,500&display=swap" rel="stylesheet">
          <link href="https://fonts.bunny.net/css?family=tajawal:400,700,900&display=swap" rel="stylesheet">
          <style>{SharedCss()}{WorkoutCss()}</style>
        </head>
        <body>
          {WorkoutCover(client, workout, coachName, coachPhone, imgLeft, imgRight)}
          {pages}
        </body>
        </html>
        """;
    }

    // ── DIET HTML BUILDER ─────────────────────────────────────────
    private string BuildDietHtml(Diet diet)
    {
        var client       = diet.Client;
        var coach        = client.Coach;
        var coachEmail   = coach?.Email ?? "";
        var coachPhone   = coach?.PhoneNumber ?? "";
        var meals        = diet.MealItems.ToList();
        int totalPages   = meals.Count;
        int totalCal     = meals.Sum(m => m.Calories);
        int totalProtein = meals.Sum(m => m.Protein);
        int totalCarbs   = meals.Sum(m => m.Carbs);
        int totalFats    = meals.Sum(m => m.Fats);

        // ── load images just like the workout cover ──
        var imgLeft  = ImageToBase64("img-left.jpeg");
        var imgRight = ImageToBase64("img-right.jpeg");

        var pages = new System.Text.StringBuilder();
        for (int i = 0; i < meals.Count; i++)
            pages.Append(MealPage(meals[i], i + 1, totalPages, client.Name, coachEmail, coachPhone));

        return $"""
        <!DOCTYPE html>
        <html lang="ar" dir="rtl">
        <head>
          <meta charset="UTF-8">
          <link href="https://fonts.bunny.net/css?family=barlow+condensed:700,900&display=swap" rel="stylesheet">
          <link href="https://fonts.bunny.net/css?family=barlow:400,500&display=swap" rel="stylesheet">
          <link href="https://fonts.bunny.net/css?family=tajawal:400,700,900&display=swap" rel="stylesheet">
          <style>{SharedCss()}{DietCss()}</style>
        </head>
        <body>
          {DietCover(client, diet, coachPhone, totalCal, totalProtein, totalCarbs, totalFats, imgLeft, imgRight)}
          {pages}
        </body>
        </html>
        """;
    }

    // ── WORKOUT COVER ─────────────────────────────────────────────
    private static string WorkoutCover(
        Client client, WorkOut workout,
        string coachName, string coachPhone,
        string imgLeft, string imgRight) => $"""
        <div class="cover-page workout-cover" dir="ltr">
          <div class="cv-header">
            <div class="cv-logo">
              <div class="cv-logo-icon">
                <svg viewBox="0 0 60 60" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <ellipse cx="30" cy="42" rx="12" ry="13" fill="white"/>
                  <circle cx="30" cy="20" r="9" fill="white"/>
                  <path d="M18 38 C6 30 8 18 16 16 C20 15 22 20 18 25 C22 28 20 34 20 38Z" fill="white"/>
                  <ellipse cx="11" cy="20" rx="5" ry="7" fill="white" transform="rotate(-20 11 20)"/>
                  <path d="M42 38 C54 30 52 18 44 16 C40 15 38 20 42 25 C38 28 40 34 40 38Z" fill="white"/>
                  <ellipse cx="49" cy="20" rx="5" ry="7" fill="white" transform="rotate(20 49 20)"/>
                </svg>
              </div>
              <div class="cv-logo-name">Atlam</div>
              <div class="cv-logo-fitness">FITNESS</div>
            </div>
            <div class="cv-x-grid">
              <span>×</span><span>×</span><span>×</span><span>×</span>
              <span>×</span><span>×</span><span>×</span><span>×</span>
              <span>×</span><span>×</span><span>×</span><span>×</span>
              <span>×</span><span>×</span><span>×</span><span>×</span>
            </div>
          </div>
          <div class="cv-photo-area">
            {(string.IsNullOrEmpty(imgLeft)  ? "" : $"<div class='photo-left'><img src='{imgLeft}' /></div>")}
            {(string.IsNullOrEmpty(imgRight) ? "" : $"<div class='photo-right'><img src='{imgRight}' /></div>")}
            <div class="cv-photo-fade"></div>
            <div class="cv-accents-left">
              <div class="cv-acc-row"><div class="cv-acc-bar"></div><div class="cv-acc-bar"></div></div>
              <div class="cv-acc-row"><div class="cv-acc-bar hollow"></div><div class="cv-acc-bar hollow"></div></div>
            </div>
            <div class="cv-chevrons">
              <svg width="62" height="40" viewBox="0 0 62 40" fill="none"><polygon points="0,0 31,22 62,0 62,14 31,36 0,14" fill="#e31c1c"/></svg>
              <svg width="62" height="28" viewBox="0 0 62 28" fill="none" style="margin-top:-2px"><polyline points="3,3 31,22 59,3" stroke="#e31c1c" stroke-width="3" stroke-linejoin="round" fill="none"/></svg>
              <svg width="56" height="24" viewBox="0 0 56 24" fill="none" style="margin-top:0;opacity:.55"><polyline points="3,3 28,18 53,3" stroke="#e31c1c" stroke-width="2.5" stroke-linejoin="round" fill="none"/></svg>
            </div>
          </div>
          <div class="cv-bottom">
            <div class="cv-bottom-inner">
              <div class="cv-red-vbar"></div>
              <div class="cv-text-block">
                <span class="cv-line1">GET FIT</span>
                <span class="cv-line2">BE STRONG</span>
                <div class="cv-client-info">
                  <div class="cv-prepared" dir="rtl">مُعد لـ <strong>{client.Name}</strong></div>
                  <div class="cv-stats">
                    <div class="cv-stat">
                      <span class="cv-stat-val">{workout.NumberOfWorkOutDays}</span>
                      <span class="cv-stat-lbl" dir="rtl">أيام</span>
                    </div>
                    <div class="cv-stat">
                      <span class="cv-stat-val">{client.SubscriptionDurationPerMonth}</span>
                      <span class="cv-stat-lbl" dir="rtl">أسبوع</span>
                    </div>
                    <div class="cv-stat">
                      <span class="cv-stat-val">{client.StartDate:yyyy}</span>
                      <span class="cv-stat-lbl" dir="rtl">بداية</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            <div class="cv-bottom-accents">
              <div class="cv-ba solid"></div><div class="cv-ba solid"></div><div class="cv-ba hollow"></div>
            </div>
            <div class="cv-footer-text">Atlam FIT · {coachName} · {coachPhone}</div>
          </div>
        </div>
        """;

    // ── DIET COVER ────────────────────────────────────────────────
    // imgLeft and imgRight added to signature so images appear on diet cover too
    private static string DietCover(
        Client client, Diet diet, string coachPhone,
        int cal, int protein, int carbs, int fats,
        string imgLeft, string imgRight) => $"""
        <div class="cover-page diet-cover" dir="ltr">
          <div class="cv-header">
            <div class="cv-logo">
              <div class="cv-logo-icon diet-icon">
                <svg viewBox="0 0 60 60" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <ellipse cx="30" cy="42" rx="12" ry="13" fill="white"/>
                  <circle cx="30" cy="20" r="9" fill="white"/>
                  <path d="M18 38 C6 30 8 18 16 16 C20 15 22 20 18 25 C22 28 20 34 20 38Z" fill="white"/>
                  <ellipse cx="11" cy="20" rx="5" ry="7" fill="white" transform="rotate(-20 11 20)"/>
                  <path d="M42 38 C54 30 52 18 44 16 C40 15 38 20 42 25 C38 28 40 34 40 38Z" fill="white"/>
                  <ellipse cx="49" cy="20" rx="5" ry="7" fill="white" transform="rotate(20 49 20)"/>
                </svg>
              </div>
              <div class="cv-logo-name diet-green">Atlam</div>
              <div class="cv-logo-fitness diet-green-sub">FITNESS</div>
            </div>
            <div class="cv-x-grid diet-x">
              <span>×</span><span>×</span><span>×</span><span>×</span>
              <span>×</span><span>×</span><span>×</span><span>×</span>
              <span>×</span><span>×</span><span>×</span><span>×</span>
              <span>×</span><span>×</span><span>×</span><span>×</span>
            </div>
          </div>
          <div class="cv-photo-area diet-photo">
            {(string.IsNullOrEmpty(imgLeft)  ? "" : $"<div class='photo-left'><img src='{imgLeft}' /></div>")}
            {(string.IsNullOrEmpty(imgRight) ? "" : $"<div class='photo-right'><img src='{imgRight}' /></div>")}
            <div class="cv-photo-fade diet-fade"></div>
            <div class="cv-accents-left">
              <div class="cv-acc-row"><div class="cv-acc-bar diet-acc"></div><div class="cv-acc-bar diet-acc"></div></div>
              <div class="cv-acc-row"><div class="cv-acc-bar hollow diet-acc-hollow"></div><div class="cv-acc-bar hollow diet-acc-hollow"></div></div>
            </div>
            <div class="cv-chevrons">
              <svg width="62" height="40" viewBox="0 0 62 40" fill="none"><polygon points="0,0 31,22 62,0 62,14 31,36 0,14" fill="#1a8c3c"/></svg>
              <svg width="62" height="28" viewBox="0 0 62 28" fill="none" style="margin-top:-2px"><polyline points="3,3 31,22 59,3" stroke="#1a8c3c" stroke-width="3" stroke-linejoin="round" fill="none"/></svg>
              <svg width="56" height="24" viewBox="0 0 56 24" fill="none" style="margin-top:0;opacity:.55"><polyline points="3,3 28,18 53,3" stroke="#1a8c3c" stroke-width="2.5" stroke-linejoin="round" fill="none"/></svg>
            </div>
          </div>
          <div class="cv-bottom">
            <div class="cv-bottom-inner">
              <div class="cv-red-vbar diet-vbar"></div>
              <div class="cv-text-block">
                <span class="cv-line1">EAT SMART</span>
                <span class="cv-line2 diet-line2">LIVE STRONG</span>
                <div class="cv-client-info">
                  <div class="cv-prepared" dir="rtl">مُعد لـ <strong>{client.Name}</strong></div>
                  <div class="cv-stats">
                    <div class="cv-stat">
                      <span class="cv-stat-val diet-green">{diet.NumberOfMeals}</span>
                      <span class="cv-stat-lbl" dir="rtl">وجبات</span>
                    </div>
                    <div class="cv-stat">
                      <span class="cv-stat-val diet-green">{cal}</span>
                      <span class="cv-stat-lbl" dir="rtl">سعرة/يوم</span>
                    </div>
                    <div class="cv-stat">
                      <span class="cv-stat-val diet-green">{protein}g</span>
                      <span class="cv-stat-lbl" dir="rtl">بروتين</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            <div class="cv-bottom-accents">
              <div class="cv-ba diet-solid"></div><div class="cv-ba diet-solid"></div><div class="cv-ba diet-hollow"></div>
            </div>
            <div class="cv-footer-text">Atlam FIT · {coachPhone}</div>
          </div>
        </div>
        """;

    // ── WORKOUT DAY PAGE ──────────────────────────────────────────
    private static string WorkoutDayPage(
        WorkOutDay day, string clientName,
        string email, string phone,
        int pageNum, int totalPages)
    {
        var rows = new System.Text.StringBuilder();
        foreach (var ex in day.ExcerciseItems)
            rows.Append($"""
                <tr>
                  <td>{(string.IsNullOrEmpty(ex.ExcerciseLink) ? ex.ExerciseName : $"<a href='{ex.ExcerciseLink}' style='color:#e31c1c;text-decoration:none;'>{ex.ExerciseName}</a>")}</td>
                  <td class="num-cell">{ex.Sets}</td>
                  <td class="num-cell">{ex.Reps}</td>
                  <td>{ex.RestTime}</td>
                </tr>
                """);

        var cardioSection = "";
        if (day.Cardio != null)
        {
            var c = day.Cardio;
            cardioSection = $"""
                <div class="cardio-section">
                  <div class="cardio-header">
                    <div class="cardio-icon">▶</div>
                    <div class="cardio-title">كارديو — {c.CardioType}</div>
                  </div>
                  <table class="plan-table cardio-table">
                    <thead>
                      <tr style="background:#1a0505;">
                        <th>النوع</th><th>المدة</th><th>الشدة</th><th>ملاحظات</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr>
                        <td>{c.CardioType}</td>
                        <td class="num-cell">{c.DurationMinutes} د</td>
                        <td class="num-cell">{c.Intensity}</td>
                        <td>{c.Notes ?? "—"}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
                """;
        }

        return $"""
        <div class="pdf-page">
          <div class="page-header">
            <div class="header-left">
              <div class="page-logo-icon">
                <svg viewBox="0 0 60 60" fill="none" width="28" height="28">
                  <ellipse cx="30" cy="42" rx="12" ry="13" fill="white"/>
                  <circle cx="30" cy="20" r="9" fill="white"/>
                  <path d="M18 38 C6 30 8 18 16 16 C20 15 22 20 18 25 C22 28 20 34 20 38Z" fill="white"/>
                  <ellipse cx="11" cy="20" rx="5" ry="7" fill="white" transform="rotate(-20 11 20)"/>
                  <path d="M42 38 C54 30 52 18 44 16 C40 15 38 20 42 25 C38 28 40 34 40 38Z" fill="white"/>
                  <ellipse cx="49" cy="20" rx="5" ry="7" fill="white" transform="rotate(20 49 20)"/>
                </svg>
              </div>
              <div>
                <div class="brand-name">ATLAM FIT</div>
                <div class="brand-sub">Weekly Training Program</div>
              </div>
            </div>
            <div class="page-meta">
              <div class="plan-label">WORKOUT PLAN</div>
              <div class="client-name">{clientName}</div>
              <div class="page-num">صفحة {pageNum} / {totalPages}</div>
            </div>
          </div>
          <div class="day-section">
            <div class="day-header">
              <div class="day-badge">{day.Day}</div>
              <div class="day-title">{day.DayName}</div>
            </div>
            <table class="plan-table">
              <thead>
                <tr><th>التمرين</th><th>المجموعات</th><th>العدد</th><th>الراحة</th></tr>
              </thead>
              <tbody>{rows}</tbody>
            </table>
            {cardioSection}
          </div>
          <div class="page-footer">
            <div class="footer-brand">ATLAM FIT</div>
            <div class="footer-divider"></div>
            <div class="footer-contact">{email} · {phone}</div>
          </div>
        </div>
        """;
    }

    // ── MEAL PAGE ─────────────────────────────────────────────────
    private static string MealPage(
        MealItem meal, int pageNum, int totalPages,
        string clientName, string email, string phone)
    {
        var rows = new System.Text.StringBuilder();
        rows.Append($"""
            <tr>
              <td class="meal-name">{(string.IsNullOrEmpty(meal.Link) ? meal.MealName : $"<a href='{meal.Link}' style='color:#1a8c3c;text-decoration:none;'>{meal.MealName}</a>")}</td>
              <td>{meal.Description}</td>
              <td>{meal.Protein}g</td>
              <td>{meal.Carbs}g</td>
              <td>{meal.Fats}g</td>
              <td class="cal-cell">{meal.Calories}</td>
            </tr>
            """);

        foreach (var alt in meal.AlternativeItems ?? [])
            rows.Append($"""
                <tr class="alt-row">
                  <td class="meal-name alt-name">↳ {alt.MealName}</td>
                  <td class="alt-desc">{alt.Description}</td>
                  <td>{alt.Protein}g</td>
                  <td>{alt.Carbs}g</td>
                  <td>{alt.Fats}g</td>
                  <td class="cal-cell">{alt.Calories}</td>
                </tr>
                """);

        rows.Append($"""
            <tr class="total-row">
              <td colspan="5" class="total-label">إجمالي الوجبة</td>
              <td class="cal-cell">{meal.Calories}</td>
            </tr>
            """);

        return $"""
        <div class="pdf-page">
          <div class="page-header" style="background:#0d1f0d;border-bottom:3px solid #1a8c3c;">
            <div class="header-left">
              <div class="page-logo-icon" style="background:#1a8c3c;">
                <svg viewBox="0 0 60 60" fill="none" width="28" height="28">
                  <ellipse cx="30" cy="42" rx="12" ry="13" fill="white"/>
                  <circle cx="30" cy="20" r="9" fill="white"/>
                  <path d="M18 38 C6 30 8 18 16 16 C20 15 22 20 18 25 C22 28 20 34 20 38Z" fill="white"/>
                  <ellipse cx="11" cy="20" rx="5" ry="7" fill="white" transform="rotate(-20 11 20)"/>
                  <path d="M42 38 C54 30 52 18 44 16 C40 15 38 20 42 25 C38 28 40 34 40 38Z" fill="white"/>
                  <ellipse cx="49" cy="20" rx="5" ry="7" fill="white" transform="rotate(20 49 20)"/>
                </svg>
              </div>
              <div>
                <div class="brand-name" style="color:#4dcc80;">ATLAM FIT</div>
                <div class="brand-sub">Daily Nutrition Program</div>
              </div>
            </div>
            <div class="page-meta">
              <div class="plan-label" style="color:#4dcc80;">DIET PLAN</div>
              <div class="client-name">{clientName}</div>
              <div class="page-num">صفحة {pageNum} / {totalPages}</div>
            </div>
          </div>
          <div class="day-section">
            <div class="day-header">
              <div class="day-badge" style="background:#1a8c3c;">وجبة {pageNum}</div>
              <div class="day-title">{meal.MealName}</div>
            </div>
            <table class="plan-table">
              <thead>
                <tr style="background:#0d1f0d;">
                  <th>الوجبة / المكون</th><th>الكمية</th>
                  <th>البروتين</th><th>الكارب</th><th>الدهون</th><th>السعرات</th>
                </tr>
              </thead>
              <tbody>{rows}</tbody>
            </table>
          </div>
          <div class="page-footer" style="border-top:2px solid #1a8c3c;">
            <div class="footer-brand" style="color:#4dcc80;">ATLAM FIT</div>
            <div class="footer-divider"></div>
            <div class="footer-contact">{email} · {phone}</div>
          </div>
        </div>
        """;
    }

    // ── CSS ───────────────────────────────────────────────────────
    private static string SharedCss() => """
        *,*::before,*::after{margin:0;padding:0;box-sizing:border-box}
        body{font-family:'Tajawal',sans-serif;background:#111;color:#111}
        .cover-page{
            width:794px;height:1123px;
            background:#111111;
            position:relative;overflow:hidden;
            page-break-after:always;
        }
        .cover-page::before{
            content:'';position:absolute;inset:0;z-index:1;pointer-events:none;
            background-image:
                repeating-linear-gradient(17deg,transparent,transparent 120px,rgba(255,255,255,0.012) 120px,rgba(255,255,255,0.012) 121px),
                repeating-linear-gradient(-43deg,transparent,transparent 90px,rgba(255,255,255,0.008) 90px,rgba(255,255,255,0.008) 91px);
        }
        .cv-header{
            position:absolute;top:0;left:0;right:0;z-index:10;
            padding:28px 32px 0;
            display:flex;justify-content:space-between;align-items:flex-start;
            flex-direction:row;
        }
        .cv-logo{display:flex;flex-direction:column;align-items:flex-start;gap:0}
        .cv-logo-icon{
            width:48px;height:48px;background:#e31c1c;border-radius:50%;
            display:flex;align-items:center;justify-content:center;margin-bottom:6px;
        }
        .cv-logo-name{font-family:'Barlow Condensed',sans-serif;font-size:26px;color:#fff;letter-spacing:4px;line-height:1;font-weight:900}
        .cv-logo-fitness{font-family:'Barlow Condensed',sans-serif;font-weight:700;font-size:12px;color:#e31c1c;letter-spacing:7px;margin-top:3px}
        .cv-x-grid{display:grid;grid-template-columns:repeat(4,14px);grid-template-rows:repeat(4,14px);gap:2px}
        .cv-x-grid span{color:#e31c1c;font-size:11px;font-weight:700;line-height:14px;text-align:center}
        .cv-photo-area{
            position:absolute;top:115px;left:0;right:0;height:570px;z-index:5;
            background:linear-gradient(135deg,#1a0a0a 0%,#2a0a0a 40%,#1a1a1a 100%);
        }
        .cv-photo-fade{
            position:absolute;bottom:0;left:0;right:0;height:180px;
            background:linear-gradient(to bottom,transparent 0%,#111111 100%);z-index:6;
        }
        .cv-accents-left{position:absolute;bottom:62px;left:24px;z-index:8;display:flex;flex-direction:column;gap:7px}
        .cv-acc-row{display:flex;gap:6px}
        .cv-acc-bar{width:30px;height:4px;background:#e31c1c;transform:skewX(-22deg)}
        .cv-acc-bar.hollow{background:transparent;border:2px solid #e31c1c}
        .cv-chevrons{position:absolute;right:28px;bottom:55px;z-index:8;display:flex;flex-direction:column;align-items:flex-end;gap:0}
        .cv-bottom{position:absolute;bottom:0;left:0;right:0;z-index:10;padding:0 0 38px 0}
        .cv-bottom-inner{display:flex;align-items:stretch;padding-left:32px;flex-direction:row;}
        .cv-red-vbar{width:5px;background:#e31c1c;margin-right:20px;border-radius:1px;flex-shrink:0}
        .cv-text-block{flex:1}
        .cv-line1{
            font-family:'Barlow Condensed',sans-serif;font-style:italic;font-weight:900;
            font-size:108px;color:#ffffff;text-transform:uppercase;letter-spacing:-2px;line-height:.88;display:block;
        }
        .cv-line2{
            font-family:'Barlow Condensed',sans-serif;font-style:italic;font-weight:900;
            font-size:108px;color:#e31c1c;text-transform:uppercase;letter-spacing:-2px;line-height:.88;display:block;
        }
        .cv-client-info{margin-top:18px}
        .cv-prepared{font-family:'Tajawal',sans-serif;font-size:15px;color:rgba(255,255,255,0.7);margin-bottom:12px}
        .cv-prepared strong{color:#fff;font-size:20px}
        .cv-stats{display:flex;gap:32px;flex-direction:row;}
        .cv-stat{display:flex;flex-direction:column;align-items:center}
        .cv-stat-val{font-family:'Barlow Condensed',sans-serif;font-size:22px;font-weight:900;color:#e31c1c}
        .cv-stat-lbl{font-family:'Tajawal',sans-serif;font-size:11px;color:rgba(255,255,255,0.4);letter-spacing:1px;text-transform:uppercase;margin-top:2px}
        .cv-bottom-accents{position:absolute;bottom:36px;right:28px;display:flex;gap:7px;z-index:11;flex-direction:row;}
        .cv-ba{width:22px;height:4px;transform:skewX(-22deg)}
        .cv-ba.solid{background:#e31c1c}
        .cv-ba.hollow{background:transparent;border:2px solid #e31c1c}
        .cv-footer-text{font-family:'Tajawal',sans-serif;font-size:11px;color:rgba(255,255,255,0.25);letter-spacing:1px;padding-left:32px;margin-top:12px}
        .pdf-page{
            background:#111;color:#eee;
            width:794px;min-height:1123px;
            display:flex;flex-direction:column;
            page-break-after:always;
        }
        .page-header{
            background:#1a0505;
            padding:22px 36px;
            display:flex;align-items:center;justify-content:space-between;
            border-bottom:3px solid #e31c1c;
        }
        .header-left{display:flex;align-items:center;gap:14px}
        .page-logo-icon{
            width:42px;height:42px;background:#e31c1c;border-radius:50%;
            display:flex;align-items:center;justify-content:center;flex-shrink:0;
        }
        .brand-name{font-family:'Barlow Condensed',sans-serif;font-size:22px;font-weight:900;color:#e31c1c;letter-spacing:3px}
        .brand-sub{font-size:11px;color:rgba(255,255,255,0.45);letter-spacing:1px;text-transform:uppercase;margin-top:2px}
        .page-meta{text-align:start;display:flex;flex-direction:column;gap:3px}
        .plan-label{font-size:10px;color:#e31c1c;font-weight:700;letter-spacing:2px;text-transform:uppercase}
        .client-name{font-size:17px;font-weight:700;color:#fff}
        .page-num{font-size:10px;color:rgba(255,255,255,0.35)}
        .day-section{flex:1;padding:28px 36px;display:flex;flex-direction:column;gap:20px}
        .day-header{display:flex;align-items:center;gap:14px}
        .day-badge{
            background:#e31c1c;color:#fff;font-size:11px;font-weight:700;letter-spacing:1.5px;
            padding:6px 14px;border-radius:3px;text-transform:uppercase;flex-shrink:0;
            font-family:'Barlow Condensed',sans-serif;
        }
        .day-title{font-family:'Barlow Condensed',sans-serif;font-size:26px;font-weight:900;color:#fff;text-transform:uppercase;letter-spacing:1px}
        .plan-table{width:100%;border-collapse:collapse}
        .plan-table thead tr{background:#1a0505}
        .plan-table th{
            color:#e31c1c;font-size:11px;font-weight:700;padding:12px 16px;
            text-align:center;letter-spacing:1px;text-transform:uppercase;
            border-bottom:2px solid #e31c1c;
        }
        .plan-table th:first-child{text-align:right}
        .plan-table tbody tr{border-bottom:1px solid #2a2a2a}
        .plan-table tbody tr:nth-child(even){background:#1a1a1a}
        .plan-table td{padding:13px 16px;font-size:13px;text-align:center;vertical-align:middle;color:#ccc}
        .plan-table td:first-child{text-align:right;font-weight:700;color:#fff}
        .num-cell{color:#e31c1c;font-weight:900;font-size:15px;font-family:'Barlow Condensed',sans-serif}
        .cardio-section{margin-top:8px}
        .cardio-header{display:flex;align-items:center;gap:10px;margin-bottom:10px}
        .cardio-icon{
            background:#e31c1c;color:#fff;width:26px;height:26px;border-radius:50%;
            display:flex;align-items:center;justify-content:center;font-size:10px;flex-shrink:0;
        }
        .cardio-title{font-family:'Barlow Condensed',sans-serif;font-size:20px;font-weight:900;color:#e31c1c;text-transform:uppercase;letter-spacing:1px}
        .cardio-table thead tr{background:#1a0505 !important}
        .page-footer{
            background:#0d0d0d;border-top:2px solid #2a2a2a;
            padding:12px 36px;display:flex;align-items:center;justify-content:space-between;
        }
        .footer-brand{font-size:10px;font-weight:700;color:#e31c1c;letter-spacing:2px;text-transform:uppercase;font-family:'Barlow Condensed',sans-serif}
        .footer-contact{font-size:10px;color:rgba(255,255,255,0.35);direction:ltr}
        .footer-divider{width:1px;height:18px;background:#2a2a2a}
        .photo-left{
            position:absolute;top:0;left:0;width:490px;height:100%;
            clip-path:polygon(0 0,78% 0,95% 100%,0 100%);overflow:hidden;z-index:4;
        }
        .photo-left img{
            width:100%;height:100%;object-fit:cover;object-position:center 31%;
            filter:grayscale(100%) contrast(1.15) brightness(0.9);display:block;
        }
        .photo-right{
            position:absolute;top:38px;right:0;width:490px;height:calc(100% - 38px);
            clip-path:polygon(12% 0,100% 0,100% 100%,0 100%);overflow:hidden;z-index:4;
        }
        .photo-right img{
            width:100%;height:100%;object-fit:cover;object-position:center 100%;
            filter:grayscale(100%) contrast(1.15) brightness(0.88);display:block;
        }
    """;

    private static string WorkoutCss() => """
        /* workout pages use shared dark/red theme */
    """;

    private static string DietCss() => """
        .diet-cover{background:#0a140a}
        .diet-cover .cv-photo-area{background:linear-gradient(135deg,#0a1a0a 0%,#0d2a0d 40%,#111a11 100%)}
        .diet-cover .cv-photo-fade{background:linear-gradient(to bottom,transparent 0%,#0a140a 100%)}
        .diet-icon{background:#1a8c3c !important}
        .diet-green{color:#4dcc80 !important}
        .diet-green-sub{color:#4dcc80 !important}
        .diet-x span{color:#1a8c3c !important}
        .diet-vbar{background:#1a8c3c !important}
        .diet-line2{color:#1a8c3c !important}
        .diet-acc{background:#1a8c3c !important}
        .diet-acc-hollow{border-color:#1a8c3c !important}
        .diet-solid{background:#1a8c3c !important}
        .diet-hollow{background:transparent;border:2px solid #1a8c3c}
        .meal-name{font-weight:700;color:#fff}
        .cal-cell{color:#4dcc80;font-weight:900;font-size:14px;font-family:'Barlow Condensed',sans-serif}
        .alt-row{background:#111a11}
        .alt-name{color:#4dcc80}
        .alt-desc{color:#888;font-size:12px;font-style:italic}
        .total-row{background:#0d1f0d;font-weight:700}
        .total-label{text-align:center;color:#4dcc80}
    """;
}