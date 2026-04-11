

var builder = WebApplication.CreateBuilder(args);
// After builder.Services.AddAuthorizationCore(...)

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IDietService, DietService>();
builder.Services.AddScoped<IWorkoutService, WorkoutService>();
builder.Services.AddScoped<IAlternativeService, AlternativeService>();
builder.Services.AddScoped<IPdfService, PdfService>();

// Add this so minimal APIs can read JSON bodies
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<FitPanelDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("cs")));

builder.Services.AddIdentity<PanelUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<FitPanelDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // ← changed from Always so dev works
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(Policies.AdminRole));
    options.AddPolicy(Policies.CoachOnly, policy => policy.RequireRole(Policies.CoachRole));  // ← updated
    options.AddPolicy(Policies.AdminOrCoach, policy => policy.RequireRole(Policies.AdminRole, Policies.CoachRole)); // ← updated
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
// Map all endpoints
app.MapAuthEndpoints();
app.MapClientEndpoints();
app.MapDietEndpoints();
app.MapWorkoutEndpoints();
app.UseAntiforgery();
app.MapAlternativeEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
    // Login endpoint
app.MapPost("/account/login", async (
    HttpContext context,
    SignInManager<PanelUser> signInManager,
    string? returnUrl) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();
    var rememberMe = form["rememberMe"].ToString() == "true";

    var result = await signInManager.PasswordSignInAsync(
        email, password, rememberMe, lockoutOnFailure: true);

    if (result.Succeeded)
        return Results.Redirect(returnUrl ?? "/dashboard");
    else if (result.IsLockedOut)
        return Results.Redirect("/login?error=locked");
    else
        return Results.Redirect("/login?error=invalid");
});

// Logout endpoint
app.MapPost("/logout", async (SignInManager<PanelUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/login");
}).RequireAuthorization();

// ← Seed MUST be before app.Run()
await SeedDatabase(app);

app.Run();

async Task SeedDatabase(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<PanelUser>>();

    // ← Only Admin and Coach roles
    string[] roles = [Policies.AdminRole, Policies.CoachRole];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Seed Admin
    var adminEmail = "admin@fitpanel.com";
    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var admin = new PanelUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "System Admin",
            EmailConfirmed = true,
            IsActive = true
        };
        var result = await userManager.CreateAsync(admin, "Admin@123456");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, Policies.AdminRole);
    }

    // Seed a default Coach for testing
    var coachEmail = "coach@fitpanel.com";
    if (await userManager.FindByEmailAsync(coachEmail) == null)
    {
        var coach = new PanelUser
        {
            UserName = coachEmail,
            Email = coachEmail,
            FullName = "Default Coach",
            EmailConfirmed = true,
            IsActive = true
        };
        var result = await userManager.CreateAsync(coach, "Coach@123456");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(coach, Policies.CoachRole);
    }
}