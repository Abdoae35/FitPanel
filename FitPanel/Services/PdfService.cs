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
    private static string BuildWorkoutHtml(WorkOut workout)
    {
        var client     = workout.Client;
        var coach      = client.Coach;
        var coachName  = coach?.FullName ?? "الكوتش";
        var coachEmail = coach?.Email ?? "";
        var coachPhone = coach?.PhoneNumber ?? "";
        var brandName  = $"{coachName} FIT";
        var days       = workout.WorkOutDays.OrderBy(d => d.Id).ToList();
        int totalPages = days.Count;

        var pages = new System.Text.StringBuilder();
        for (int i = 0; i < days.Count; i++)
            pages.Append(WorkoutDayPage(days[i], client.Name, coachEmail, coachPhone, i + 1, totalPages, brandName));

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
          {WorkoutCover(client, workout, coachName, coachPhone, brandName)}
          {pages}
        </body>
        </html>
        """;
    }

    // ── DIET HTML BUILDER ─────────────────────────────────────────
    private static string BuildDietHtml(Diet diet)
    {
        var client       = diet.Client;
        var coach        = client.Coach;
        var coachEmail   = coach?.Email ?? "";
        var coachPhone   = coach?.PhoneNumber ?? "";
        var coachName    = coach?.FullName ?? "الكوتش";
        var brandName    = $"{coachName} FIT";
        var meals        = diet.MealItems.ToList();
        int totalPages   = meals.Count;
        int totalCal     = meals.Sum(m => m.Calories);
        int totalProtein = meals.Sum(m => m.Protein);
        int totalCarbs   = meals.Sum(m => m.Carbs);
        int totalFats    = meals.Sum(m => m.Fats);

        var pages = new System.Text.StringBuilder();
        for (int i = 0; i < meals.Count; i++)
            pages.Append(MealPage(meals[i], i + 1, totalPages, client.Name, coachEmail, coachPhone, brandName));

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
          {DietCover(client, diet, coachPhone, totalCal, totalProtein, totalCarbs, totalFats, brandName)}
          {pages}
        </body>
        </html>
        """;
    }

    // ── COVER PAGES ───────────────────────────────────────────────
    private static string WorkoutCover(
        Client client, WorkOut workout,
        string coachName, string coachPhone,
        string brandName) => $"""
        <div class="cover-page">
          <div class="cover-glow"></div>
          <div class="cover-logo-text">{brandName}</div>
          <div class="cover-tagline">قوة · تحول · إرادة</div>
          <div class="cover-line"></div>
          <div class="cover-plan-type">برنامج التمارين الأسبوعي</div>
          <div class="cover-client">
            <div class="cover-client-label">مُعد لـ</div>
            <div class="cover-client-name">{client.Name}</div>
          </div>
          <div class="cover-details">
            <div class="cover-detail">
              <div class="cover-detail-val">{workout.NumberOfWorkOutDays}</div>
              <div class="cover-detail-lbl">أيام</div>
            </div>
            <div class="cover-detail">
              <div class="cover-detail-val">4</div>
              <div class="cover-detail-lbl">أسبوع</div>
            </div>
            <div class="cover-detail">
              <div class="cover-detail-val">{client.StartDate:yyyy}</div>
              <div class="cover-detail-lbl">بداية</div>
            </div>
          </div>
          <div class="cover-coach">{brandName} · {coachName} · {coachPhone}</div>
        </div>
        """;

    private static string DietCover(
        Client client, Diet diet, string coachPhone,
        int cal, int protein, int carbs, int fats,
        string brandName) => $"""
        <div class="cover-page diet-cover">
          <div class="cover-glow diet-glow"></div>
          <div class="cover-logo-text diet-green">{brandName}</div>
          <div class="cover-tagline">صحة · توازن · نتائج</div>
          <div class="cover-line diet-line"></div>
          <div class="cover-plan-type diet-plan-type">برنامج التغذية اليومي</div>
          <div class="cover-client">
            <div class="cover-client-label">مُعد لـ</div>
            <div class="cover-client-name">{client.Name}</div>
          </div>
          <div class="cover-details">
            <div class="cover-detail">
              <div class="cover-detail-val diet-green">{diet.NumberOfMeals}</div>
              <div class="cover-detail-lbl">وجبات</div>
            </div>
            <div class="cover-detail">
              <div class="cover-detail-val diet-green">{cal}</div>
              <div class="cover-detail-lbl">سعرة/يوم</div>
            </div>
            <div class="cover-detail">
              <div class="cover-detail-val diet-green">{protein}g</div>
              <div class="cover-detail-lbl">بروتين</div>
            </div>
          </div>
          <div class="cover-coach">{brandName} · {coachPhone}</div>
        </div>
        """;

    // ── WORKOUT DAY PAGE ──────────────────────────────────────────
    private static string WorkoutDayPage(
        WorkOutDay day, string clientName,
        string email, string phone,
        int pageNum, int totalPages,
        string brandName)
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
                <div class="brand-name">{brandName}</div>
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
            <div class="footer-brand">{brandName}</div>
            <div class="footer-divider"></div>
            <div class="footer-contact">{email} · {phone}</div>
          </div>
        </div>
        """;
    }

    // ── MEAL PAGE ─────────────────────────────────────────────────
    private static string MealPage(
        MealItem meal, int pageNum, int totalPages,
        string clientName, string email, string phone,
        string brandName)
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
                <div class="brand-name" style="color:#4dcc80;">{brandName}</div>
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
            <div class="footer-brand" style="color:#4dcc80;">{brandName}</div>
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

        /* ── COVER PAGE (matches inner page theme) ── */
        .cover-page{
            background:linear-gradient(160deg,#0d0505 0%,#1a0505 50%,#0d0505 100%);
            width:794px;min-height:1123px;
            display:flex;flex-direction:column;align-items:center;justify-content:center;
            position:relative;overflow:hidden;page-break-after:always;
        }
        .cover-glow{
            position:absolute;width:500px;height:500px;
            background:radial-gradient(circle,rgba(227,28,28,0.2) 0%,transparent 70%);
            border-radius:50%;top:50%;left:50%;transform:translate(-50%,-50%);
        }
        .cover-logo-text{font-size:64px;font-weight:900;color:#e31c1c;letter-spacing:6px;text-align:center}
        .cover-tagline{font-size:14px;color:rgba(255,255,255,0.45);letter-spacing:3px;text-transform:uppercase;text-align:center;margin-top:8px}
        .cover-line{width:60px;height:4px;background:linear-gradient(90deg,#a01010,#e31c1c);border-radius:2px;margin:20px auto}
        .cover-plan-type{
            margin-top:40px;padding:10px 32px;
            border:1.5px solid rgba(227,28,28,0.35);border-radius:4px;
            font-size:13px;font-weight:700;color:#e31c1c;letter-spacing:2px;text-transform:uppercase
        }
        .cover-client{margin-top:60px;text-align:center}
        .cover-client-label{font-size:11px;color:rgba(255,255,255,0.35);letter-spacing:2px;text-transform:uppercase;margin-bottom:8px}
        .cover-client-name{font-size:28px;font-weight:700;color:#fff}
        .cover-details{display:flex;gap:40px;margin-top:32px;justify-content:center}
        .cover-detail{text-align:center}
        .cover-detail-val{font-size:20px;font-weight:900;color:#e31c1c}
        .cover-detail-lbl{font-size:11px;color:rgba(255,255,255,0.35);letter-spacing:1px;text-transform:uppercase;margin-top:4px}
        .cover-coach{position:absolute;bottom:40px;font-size:12px;color:rgba(255,255,255,0.3);letter-spacing:1px}

        /* ── INNER PAGES ── */
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
    """;

    private static string WorkoutCss() => """
        /* workout pages use shared dark/red theme */
    """;

    private static string DietCss() => """
        /* ── OLD-DESIGN DIET COVER OVERRIDES ── */
        .diet-cover{background:linear-gradient(160deg,#061a0a 0%,#0a3d18 50%,#061a0a 100%)}
        .diet-glow{background:radial-gradient(circle,rgba(26,140,60,0.2) 0%,transparent 70%)}
        .diet-green{color:#4dcc80}
        .diet-line{background:linear-gradient(90deg,#1a8c3c,#4dcc80)}
        .diet-plan-type{border-color:rgba(77,204,128,0.35);color:#4dcc80}

        /* ── MEAL PAGES ── */
        .meal-name{font-weight:700;color:#fff}
        .cal-cell{color:#4dcc80;font-weight:900;font-size:14px;font-family:'Barlow Condensed',sans-serif}
        .alt-row{background:#111a11}
        .alt-name{color:#4dcc80}
        .alt-desc{color:#888;font-size:12px;font-style:italic}
        .total-row{background:#0d1f0d;font-weight:700}
        .total-label{text-align:center;color:#4dcc80}
    """;
}