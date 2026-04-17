using FitPanel.Data;
using FitPanel.Data.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using System.Text;

namespace FitPanel.Services;

public class TryService : IPdfService
{
    private readonly FitPanelDbContext _db;

    public TryService(FitPanelDbContext db, IWebHostEnvironment env)
    {
        _db = db;
    }

    // ── PUBLIC METHODS ────────────────────────────────────────────
    public async Task<byte[]?> GenerateWorkoutPdfAsync(int clientId, int workoutId, string coachId)
    {
        var workout = await _db.WorkOuts
            .Include(w => w.Client).ThenInclude(c => c.Coach)
            .Include(w => w.WorkOutDays).ThenInclude(d => d.ExcerciseItems)
            .Include(w => w.WorkOutDays).ThenInclude(d => d.Cardio)
            .FirstOrDefaultAsync(w => w.Id == workoutId && w.ClientId == clientId && w.Client.CoachId == coachId);

        if (workout == null) return null;
        var html = BuildWorkoutHtml(workout);
        return await RenderToPdfAsync(html);
    }

    public async Task<byte[]?> GenerateDietPdfAsync(int clientId, int dietId, string coachId)
    {
        var diet = await _db.Diets
            .Include(d => d.Client).ThenInclude(c => c.Coach)
            .Include(d => d.MealItems).ThenInclude(m => m.AlternativeItems)
            .FirstOrDefaultAsync(d => d.Id == dietId && d.ClientId == clientId && d.Client.CoachId == coachId);

        if (diet == null) return null;
        var html = BuildDietHtml(diet);
        return await RenderToPdfAsync(html);
    }

    private static async Task<byte[]> RenderToPdfAsync(string html)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync(html, new PageSetContentOptions { WaitUntil = WaitUntilState.NetworkIdle });
        return await page.PdfAsync(new PagePdfOptions
        {
            Format = "A4",
            PrintBackground = true,
            Margin = new Margin { Top = "0", Bottom = "0", Left = "0", Right = "0" }
        });
    }

    // ── HTML BUILDERS ─────────────────────────────────────────────
    private static string BuildWorkoutHtml(WorkOut workout)
    {
        var client = workout.Client;
        var coachName = client.Coach?.FullName ?? "الكوتش";
        var coachEmail = client.Coach?.Email ?? "";
        var coachPhone = client.Coach?.PhoneNumber ?? "";
        var brandName = $"{coachName} FIT";
        var days = workout.WorkOutDays.OrderBy(d => d.Id).ToList();
        
        var pages = new StringBuilder();
        for (int i = 0; i < days.Count; i++)
            pages.Append(WorkoutDayPage(days[i], client.Name, coachEmail, coachPhone, i + 1, days.Count, brandName));

        return $"""
        <!DOCTYPE html>
        <html lang="ar" dir="rtl">
        <head>
          <meta charset="UTF-8">
          <link href="https://fonts.bunny.net/css?family=barlow-condensed:700,900|barlow:400,500|tajawal:400,700,900&display=swap" rel="stylesheet">
          <style>{SharedCss()}{WorkoutCss()}</style>
        </head>
        <body class="workout-theme">
          {WorkoutCover(client, workout, coachName, coachPhone, brandName)}
          {pages}
        </body>
        </html>
        """;
    }

    private static string BuildDietHtml(Diet diet)
    {
        var client = diet.Client;
        var coachName = client.Coach?.FullName ?? "الكوتش";
        var coachEmail = client.Coach?.Email ?? "";
        var coachPhone = client.Coach?.PhoneNumber ?? "";
        var brandName = $"{coachName} FIT";
        var meals = diet.MealItems.ToList();
        
        var pages = new StringBuilder();
        for (int i = 0; i < meals.Count; i++)
            pages.Append(MealPage(meals[i], i + 1, meals.Count, client.Name, coachEmail, coachPhone, brandName));

        return $"""
        <!DOCTYPE html>
        <html lang="ar" dir="rtl">
        <head>
          <meta charset="UTF-8">
          <link href="https://fonts.bunny.net/css?family=barlow-condensed:700,900|barlow:400,500|tajawal:400,700,900&display=swap" rel="stylesheet">
          <style>{SharedCss()}{DietCss()}</style>
        </head>
        <body class="diet-theme">
          {DietCover(client, diet, coachPhone, meals.Sum(m=>m.Calories), meals.Sum(m=>m.Protein), meals.Sum(m=>m.Carbs), meals.Sum(m=>m.Fats), brandName)}
          {pages}
        </body>
        </html>
        """;
    }

    // ── COVER PAGES ───────────────────────────────────────────────
    private static string WorkoutCover(Client client, WorkOut workout, string coachName, string phone, string brand) => $"""
        <div class="cover-page">
          <div class="cover-glow"></div>
          <div class="cover-logo-text">{brand}</div>
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
          </div>
          <div class="cover-coach">{brand} · {coachName} · {phone}</div>
        </div>
        """;

    private static string DietCover(Client client, Diet diet, string phone, int cal, int p, int c, int f, string brand) => $"""
        <div class="cover-page">
          <div class="cover-glow"></div>
          <div class="cover-logo-text">{brand}</div>
          <div class="cover-tagline">صحة · توازن · نتائج</div>
          <div class="cover-line"></div>
          <div class="cover-plan-type">برنامج التغذية اليومي</div>
          <div class="cover-client">
            <div class="cover-client-label">مُعد لـ</div>
            <div class="cover-client-name">{client.Name}</div>
          </div>
          <div class="cover-details">
            <div class="cover-detail">
              <div class="cover-detail-val">{cal}</div>
              <div class="cover-detail-lbl">سعرة</div>
            </div>
            <div class="cover-detail">
              <div class="cover-detail-val">{p}g</div>
              <div class="cover-detail-lbl">بروتين</div>
            </div>
          </div>
          <div class="cover-coach">{brand} · {phone}</div>
        </div>
        """;

    // ── INNER PAGES ───────────────────────────────────────────────
    private static string WorkoutDayPage(WorkOutDay day, string client, string email, string phone, int num, int total, string brand) 
    {
        var rows = new StringBuilder();
        foreach (var ex in day.ExcerciseItems)
            rows.Append($"<tr><td>{ex.ExerciseName}</td><td class='num-cell'>{ex.Sets}</td><td class='num-cell'>{ex.Reps}</td><td>{ex.RestTime}</td></tr>");

        return $"""
        <div class="pdf-page">
          <div class="page-header">
            <div class="header-left"><div class="brand-name">{brand}</div><div class="brand-sub">Weekly Training Program</div></div>
            <div class="page-meta"><div class="plan-label">WORKOUT PLAN</div><div class="client-name">{client}</div><div class="page-num">صفحة {num} / {total}</div></div>
          </div>
          <div class="day-section">
            <div class="day-header"><div class="day-badge">{day.Day}</div><div class="day-title">{day.DayName}</div></div>
            <table class="plan-table">
              <thead><tr><th>التمرين</th><th>المجموعات</th><th>العدد</th><th>الراحة</th></tr></thead>
              <tbody>{rows}</tbody>
            </table>
          </div>
          <div class="page-footer"><div class="footer-brand">{brand}</div><div class="footer-contact">{email} · {phone}</div></div>
        </div>
        """;
    }

    private static string MealPage(MealItem meal, int num, int total, string client, string email, string phone, string brand)
    {
        var rows = new StringBuilder();
        rows.Append($"<tr><td class='meal-name'>{meal.MealName}</td><td>{meal.Description}</td><td>{meal.Protein}g</td><td>{meal.Carbs}g</td><td>{meal.Fats}g</td><td class='cal-cell'>{meal.Calories}</td></tr>");
        
        return $"""
        <div class="pdf-page">
          <div class="page-header">
            <div class="header-left"><div class="brand-name">{brand}</div><div class="brand-sub">Daily Nutrition Program</div></div>
            <div class="page-meta"><div class="plan-label">DIET PLAN</div><div class="client-name">{client}</div><div class="page-num">صفحة {num} / {total}</div></div>
          </div>
          <div class="day-section">
            <div class="day-header"><div class="day-badge">وجبة {num}</div><div class="day-title">{meal.MealName}</div></div>
            <table class="plan-table">
              <thead><tr><th>الوجبة / المكون</th><th>الكمية</th><th>بروتين</th><th>كارب</th><th>دهون</th><th>سعرات</th></tr></thead>
              <tbody>{rows}</tbody>
            </table>
          </div>
          <div class="page-footer"><div class="footer-brand">{brand}</div><div class="footer-contact">{email} · {phone}</div></div>
        </div>
        """;
    }

    // ── CSS STYLES ────────────────────────────────────────────────
    private static string SharedCss() => """
        *,*::before,*::after{margin:0;padding:0;box-sizing:border-box}
        body{font-family:'Tajawal',sans-serif;background:#111;color:#eee}

        /* --- COVER PAGE (Customized) --- */
        .cover-page {
            background: var(--cover-bg); width:794px; min-height:1123px;
            display:flex; flex-direction:column; align-items:center; justify-content:center;
            position:relative; overflow:hidden; page-break-after:always;
        }
        .cover-glow {
            position:absolute; width:600px; height:600px;
            background:radial-gradient(circle, var(--glow) 0%, transparent 70%);
            top:50%; left:50%; transform:translate(-50%,-50%);
        }
        .cover-logo-text { font-size:64px; font-weight:900; color:var(--accent); letter-spacing:6px; z-index:1; }
        .cover-tagline { font-size:14px; color:rgba(255,255,255,0.4); letter-spacing:3px; margin-top:8px; z-index:1; }
        .cover-line { width:60px; height:4px; background:var(--accent); margin:20px 0; z-index:1; border-radius:2px; }
        .cover-plan-type { 
            margin-top:40px; padding:10px 32px; font-size:13px; font-weight:700; 
            color:var(--accent); letter-spacing:2px; z-index:1;
            border: none; /* REMOVED BORDER AS REQUESTED */
        }
        .cover-client { margin-top:60px; text-align:center; z-index:1; }
        .cover-client-label { font-size:11px; color:rgba(255,255,255,0.3); margin-bottom:8px; }
        .cover-client-name { font-size:32px; font-weight:700; color:#fff; }
        .cover-details { display:flex; gap:40px; margin-top:32px; z-index:1; }
        .cover-detail { text-align:center; }
        .cover-detail-val { font-size:24px; font-weight:900; color:var(--accent); }
        .cover-detail-lbl { font-size:11px; color:rgba(255,255,255,0.3); margin-top:4px; }
        .cover-coach { position:absolute; bottom:40px; font-size:12px; color:rgba(255,255,255,0.3); }

        /* --- INNER PAGES (Original Design Restored) --- */
        .pdf-page { background:#111; width:794px; min-height:1123px; display:flex; flex-direction:column; page-break-after:always; }
        .page-header { background:var(--inner-header); padding:22px 36px; display:flex; align-items:center; justify-content:space-between; border-bottom:3px solid var(--accent); }
        .brand-name { font-family:'Barlow Condensed', sans-serif; font-size:22px; font-weight:900; color:var(--accent); letter-spacing:3px; }
        .brand-sub { font-size:11px; color:rgba(255,255,255,0.45); margin-top:2px; }
        .page-meta { text-align:start; display:flex; flex-direction:column; gap:3px; }
        .plan-label { font-size:10px; color:var(--accent); font-weight:700; letter-spacing:2px; }
        .client-name { font-size:17px; font-weight:700; color:#fff; }
        .page-num { font-size:10px; color:rgba(255,255,255,0.35); }
        .day-section { flex:1; padding:28px 36px; display:flex; flex-direction:column; gap:20px; }
        .day-header { display:flex; align-items:center; gap:14px; }
        .day-badge { background:var(--accent); color:#fff; font-size:11px; font-weight:700; padding:6px 14px; border-radius:3px; font-family:'Barlow Condensed'; }
        .day-title { font-family:'Barlow Condensed'; font-size:26px; font-weight:900; color:#fff; letter-spacing:1px; }
        .plan-table { width:100%; border-collapse:collapse; }
        .plan-table thead tr { background:var(--inner-header); }
        .plan-table th { color:var(--accent); font-size:11px; font-weight:700; padding:12px 16px; border-bottom:2px solid var(--accent); text-align:center; }
        .plan-table td { padding:13px 16px; font-size:13px; text-align:center; color:#ccc; border-bottom:1px solid #2a2a2a; }
        .plan-table td:first-child { text-align:right; font-weight:700; color:#fff; }
        .num-cell { color:var(--accent); font-weight:900; font-size:15px; font-family:'Barlow Condensed'; }
        .page-footer { background:#0d0d0d; border-top:2px solid #2a2a2a; padding:12px 36px; display:flex; justify-content:space-between; font-size:10px; }
        .footer-brand { font-weight:700; color:var(--accent); font-family:'Barlow Condensed'; }
        .footer-contact { color:rgba(255,255,255,0.35); }
        """;

    private static string WorkoutCss() => """
        .workout-theme {
            --accent: #e31c1c;
            --glow: rgba(227, 28, 28, 0.2);
            --cover-bg: linear-gradient(160deg, #0f0505 0%, #1a0505 100%);
            --inner-header: #1a0505;
        }
        """;

    private static string DietCss() => """
        .diet-theme {
            --accent: #1a8c3c;
            --glow: rgba(26, 140, 60, 0.2);
            --cover-bg: linear-gradient(160deg, #050f05 0%, #051a05 100%);
            --inner-header: #051a05;
        }
        .cal-cell { color: var(--accent); font-weight: 900; }
        """;
}