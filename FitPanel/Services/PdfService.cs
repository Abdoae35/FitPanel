using FitPanel.Data;
using FitPanel.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
namespace FitPanel.Services;
public class PdfService : IPdfService
{
    private readonly FitPanelDbContext _db;
    public PdfService(FitPanelDbContext db)
    {
        _db = db;
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
            Margin = new Margin
            {
                Top    = "0",
                Bottom = "0",
                Left   = "0",
                Right  = "0",
            }
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
        var days       = workout.WorkOutDays.OrderBy(d => d.Id).ToList();
        int totalPages = days.Count + days.Count(d => d.Cardio != null);
        var pages = new System.Text.StringBuilder();
        int pageNum = 1;
        foreach (var day in days)
        {
            pages.Append(WorkoutDayPage(
                day, client.Name, coachEmail, coachPhone,
                pageNum, totalPages));
            pageNum++;
            if (day.Cardio != null)
            {
                pages.Append(CardioPage(
                    day, client.Name, coachEmail, coachPhone,
                    pageNum, totalPages));
                pageNum++;
            }
        }
        return $"""
        <!DOCTYPE html>
        <html lang="ar" dir="rtl">
        <head>
          <meta charset="UTF-8">
          <link href="https://fonts.bunny.net/css?family=tajawal:400,700,900&display=swap" rel="stylesheet">
          <style>{SharedCss()}{WorkoutCss()}</style>
        </head>
        <body>
          {WorkoutCover(client, workout, coachName, coachPhone)}
          {pages}
        </body>
        </html>
        """;
    }
    // ── DIET HTML BUILDER ─────────────────────────────────────────
    private static string BuildDietHtml(Diet diet)
    {
        var client     = diet.Client;
        var coach      = client.Coach;
        var coachEmail = coach?.Email ?? "";
        var coachPhone = coach?.PhoneNumber ?? "";
        var meals      = diet.MealItems.ToList();
        int totalPages = meals.Count;
        int totalCal     = meals.Sum(m => m.Calories);
        int totalProtein = meals.Sum(m => m.Protein);
        int totalCarbs   = meals.Sum(m => m.Carbs);
        int totalFats    = meals.Sum(m => m.Fats);
        var pages = new System.Text.StringBuilder();
        for (int i = 0; i < meals.Count; i++)
            pages.Append(MealPage(
                meals[i], i + 1, totalPages,
                client.Name, coachEmail, coachPhone));
        return $"""
        <!DOCTYPE html>
        <html lang="ar" dir="rtl">
        <head>
          <meta charset="UTF-8">
          <link href="https://fonts.bunny.net/css?family=tajawal:400,700,900&display=swap" rel="stylesheet">
          <style>{SharedCss()}{DietCss()}</style>
        </head>
        <body>
          {DietCover(client, diet, coachPhone,
              totalCal, totalProtein, totalCarbs, totalFats)}
          {pages}
        </body>
        </html>
        """;
    }
    // ── COVER PAGES ───────────────────────────────────────────────
    private static string WorkoutCover(
        Client client, WorkOut workout,
        string coachName, string coachPhone) => $"""
        <div class="cover-page">
          <div class="cover-glow"></div>
          <div class="cover-logo-text">Atlam FIT</div>
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
              <div class="cover-detail-val">{client.SubscriptionDurationPerMonth}</div>
              <div class="cover-detail-lbl">أسبوع</div>
            </div>
            <div class="cover-detail">
              <div class="cover-detail-val">{client.StartDate:yyyy}</div>
              <div class="cover-detail-lbl">بداية</div>
            </div>
          </div>
          <div class="cover-coach">Atlam FIT · {coachName} · {coachPhone}</div>
        </div>
        """;
    private static string DietCover(
        Client client, Diet diet, string coachPhone,
        int cal, int protein, int carbs, int fats) => $"""
        <div class="cover-page diet-cover">
          <div class="cover-glow diet-glow"></div>
          <div class="cover-logo-text diet-green">Atlam FIT</div>
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
          <div class="cover-coach">Atlam FIT · {coachPhone}</div>
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
                  <td><a href="{ex.ExcerciseLink}" target="_blank">{ex.ExerciseName}</a></td>
                  <td class="num-cell">{ex.Sets}</td>
                  <td class="num-cell">{ex.Reps}</td>
                  <td>{ex.RestTime}</td>
                </tr>
                """);
        return $"""
        <div class="pdf-page">
          <div class="page-header">
            <div class="brand-block">
              <div class="brand-name">Atlam FIT</div>
              <div class="brand-sub">Weekly Training Program</div>
            </div>
            <div class="page-meta">
              <div class="plan-label">Workout Plan</div>
              <div class="client-name">{clientName}</div>
              <div class="page-num">صفحة {pageNum} / {totalPages}</div>
            </div>
          </div>
          <div class="day-section">
            <div class="day-header">
              <div class="day-badge">{day.Day}</div>
              <div>
                <div class="day-title">{day.DayName}</div>
              </div>
            </div>
            <table class="plan-table">
              <thead>
                <tr><th>التمرين</th><th>المجموعات</th><th>العدد</th><th>الراحة</th></tr>
              </thead>
              <tbody>{rows}</tbody>
            </table>
          </div>
          <div class="page-footer">
            <div class="footer-brand">Atlam FIT</div>
            <div class="footer-divider"></div>
            <div class="footer-contact">{email} · {phone}</div>
          </div>
        </div>
        """;
    }
    // ── CARDIO PAGE ───────────────────────────────────────────────
    private static string CardioPage(
        WorkOutDay day, string clientName,
        string email, string phone,
        int pageNum, int totalPages)
    {
        var c = day.Cardio!;
        return $"""
        <div class="pdf-page">
          <div class="page-header">
            <div class="brand-block">
              <div class="brand-name">Atlam FIT</div>
              <div class="brand-sub">Cardio Session</div>
            </div>
            <div class="page-meta">
              <div class="plan-label">Cardio</div>
              <div class="client-name">{clientName}</div>
              <div class="page-num">صفحة {pageNum} / {totalPages}</div>
            </div>
          </div>
          <div class="day-section">
            <div class="day-header">
              <div class="day-badge">{day.Day} — كارديو</div>
              <div><div class="day-title">{c.CardioType}</div></div>
            </div>
            <table class="plan-table">
              <thead>
                <tr><th>النوع</th><th>المدة</th><th>الشدة</th><th>ملاحظات</th></tr>
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
          <div class="page-footer">
            <div class="footer-brand">Atlam FIT</div>
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
              <td class="meal-name">{meal.MealName}</td>
              <td><a href="{meal.Link}" target="_blank">{meal.Description}</a></td>
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
          <div class="page-header" style="background:linear-gradient(135deg,#061a0a,#0d3d18);border-bottom:3px solid #4dcc80;">
            <div class="brand-block">
              <div class="brand-name diet-green">Atlam FIT</div>
              <div class="brand-sub">Daily Nutrition Program</div>
            </div>
            <div class="page-meta">
              <div class="plan-label diet-green">Diet Plan</div>
              <div class="client-name">{clientName}</div>
              <div class="page-num">صفحة {pageNum} / {totalPages}</div>
            </div>
          </div>
          <div class="day-section">
            <div class="day-header">
              <div class="day-badge diet-badge">وجبة {pageNum}</div>
              <div><div class="day-title">{meal.MealName}</div></div>
            </div>
            <table class="plan-table">
              <thead>
                <tr style="background:linear-gradient(135deg,#061a0a,#0d3d18);">
                  <th>الوجبة / المكون</th><th>الكمية</th>
                  <th>البروتين</th><th>الكارب</th><th>الدهون</th><th>السعرات</th>
                </tr>
              </thead>
              <tbody>{rows}</tbody>
            </table>
          </div>
          <div class="page-footer">
            <div class="footer-brand diet-green">Atlam FIT</div>
            <div class="footer-divider"></div>
            <div class="footer-contact">{email} · {phone}</div>
          </div>
        </div>
        """;
    }
    // ── CSS ───────────────────────────────────────────────────────
    private static string SharedCss() => """
        *,*::before,*::after{margin:0;padding:0;box-sizing:border-box}
        body{font-family:'Tajawal',sans-serif;background:#fff;color:#111}
        .pdf-page{
            background:#fff;color:#111;
            width:794px;min-height:1123px;
            display:flex;flex-direction:column;
            page-break-after:always;
        }
        .page-header{
            background:linear-gradient(135deg,#0a1f3d 0%,#0d2d5a 60%,#0a2040 100%);
            padding:28px 40px 22px;
            display:flex;align-items:center;justify-content:space-between;
            border-bottom:3px solid #1a5aff;
        }
        .brand-block{display:flex;flex-direction:column;gap:4px}
        .brand-name{font-size:26px;font-weight:900;color:#4da6ff;letter-spacing:2px}
        .brand-sub{font-size:12px;color:rgba(255,255,255,0.55);letter-spacing:1px;text-transform:uppercase}
        .page-meta{text-align:start;display:flex;flex-direction:column;gap:4px}
        .plan-label{font-size:11px;color:#4da6ff;font-weight:700;letter-spacing:1.5px;text-transform:uppercase}
        .client-name{font-size:18px;font-weight:700;color:#fff}
        .page-num{font-size:11px;color:rgba(255,255,255,0.4)}
        .day-section{flex:1;padding:32px 40px;display:flex;flex-direction:column}
        .day-header{display:flex;align-items:center;gap:16px;margin-bottom:24px}
        .day-badge{
            background:linear-gradient(135deg,#1a5aff,#0d3dbf);
            color:#fff;font-size:12px;font-weight:700;letter-spacing:1.5px;
            padding:6px 14px;border-radius:4px;text-transform:uppercase;flex-shrink:0
        }
        .day-title{font-size:22px;font-weight:900;color:#0a1f3d}
        .plan-table{width:100%;border-collapse:collapse;flex:1}
        .plan-table thead tr{background:linear-gradient(135deg,#0a1f3d,#0d2d5a)}
        .plan-table th{color:#fff;font-size:13px;font-weight:700;padding:14px 18px;text-align:center;letter-spacing:.8px;text-transform:uppercase}
        .plan-table th:first-child{text-align:right}
        .plan-table tbody tr{border-bottom:1px solid #eaeef5}
        .plan-table tbody tr:nth-child(even){background:#f8faff}
        .plan-table td{padding:15px 18px;font-size:14px;text-align:center;vertical-align:middle;color:#222}
        .plan-table td:first-child{text-align:right;font-weight:700;color:#0a1f3d}
        .num-cell{color:#1a5aff;font-weight:900;font-size:16px}
        .page-footer{
            background:#f4f6fb;border-top:2px solid #e0e6f0;
            padding:14px 40px;display:flex;align-items:center;justify-content:space-between;
        }
        .footer-brand{font-size:11px;font-weight:700;color:#1a5aff;letter-spacing:1.5px;text-transform:uppercase}
        .footer-contact{font-size:11px;color:#888;direction:ltr}
        .footer-divider{width:1px;height:20px;background:#d0d8e8}
        .cover-page{
            background:linear-gradient(160deg,#060d1a 0%,#0a1f3d 50%,#060d1a 100%);
            width:794px;min-height:1123px;
            display:flex;flex-direction:column;align-items:center;justify-content:center;
            position:relative;overflow:hidden;page-break-after:always;
        }
        .cover-glow{
            position:absolute;width:500px;height:500px;
            background:radial-gradient(circle,rgba(26,90,255,0.2) 0%,transparent 70%);
            border-radius:50%;top:50%;left:50%;transform:translate(-50%,-50%);
        }
        .cover-logo-text{font-size:64px;font-weight:900;color:#4da6ff;letter-spacing:6px;text-align:center}
        .cover-tagline{font-size:14px;color:rgba(255,255,255,0.45);letter-spacing:3px;text-transform:uppercase;text-align:center;margin-top:8px}
        .cover-line{width:60px;height:4px;background:linear-gradient(90deg,#1a5aff,#4da6ff);border-radius:2px;margin:20px auto}
        .cover-plan-type{
            margin-top:40px;padding:10px 32px;
            border:1.5px solid rgba(77,166,255,0.35);border-radius:4px;
            font-size:13px;font-weight:700;color:#4da6ff;letter-spacing:2px;text-transform:uppercase
        }
        .cover-client{margin-top:60px;text-align:center}
        .cover-client-label{font-size:11px;color:rgba(255,255,255,0.35);letter-spacing:2px;text-transform:uppercase;margin-bottom:8px}
        .cover-client-name{font-size:28px;font-weight:700;color:#fff}
        .cover-details{display:flex;gap:40px;margin-top:32px;justify-content:center}
        .cover-detail{text-align:center}
        .cover-detail-val{font-size:20px;font-weight:900;color:#4da6ff}
        .cover-detail-lbl{font-size:11px;color:rgba(255,255,255,0.35);letter-spacing:1px;text-transform:uppercase;margin-top:4px}
        .cover-coach{position:absolute;bottom:40px;font-size:12px;color:rgba(255,255,255,0.3);letter-spacing:1px}
    """;
    private static string WorkoutCss() => """
        /* workout-specific — nothing extra needed, shared covers it */
    """;
    private static string DietCss() => """
        .diet-cover{background:linear-gradient(160deg,#061a0a 0%,#0a3d18 50%,#061a0a 100%)}
        .diet-glow{background:radial-gradient(circle,rgba(26,140,60,0.2) 0%,transparent 70%)}
        .diet-green{color:#4dcc80}
        .diet-line{background:linear-gradient(90deg,#1a8c3c,#4dcc80)}
        .diet-plan-type{border-color:rgba(77,204,128,0.35);color:#4dcc80}
        .diet-badge{background:linear-gradient(135deg,#1a8c3c,#0d5c24)}
        .meal-name{font-weight:700;color:#0a3d1a}
        .cal-cell{color:#d45a00;font-weight:900;font-size:15px}
        .alt-row{background:#fffbf0}
        .alt-name{color:#d45a00}
        .alt-desc{color:#888;font-size:12px;font-style:italic}
        .total-row{background:#f0faf4;font-weight:700}
        .total-label{text-align:center;color:#1a8c3c}
    """;
}