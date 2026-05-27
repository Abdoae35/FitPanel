// Data/FitPanelDbContext.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FitPanel.Data;

public class FitPanelDbContext : IdentityDbContext<PanelUser>
{
    public FitPanelDbContext(DbContextOptions<FitPanelDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Coach → Clients (one to many)
        modelBuilder.Entity<Client>()
            .HasOne(c => c.Coach)
            .WithMany(u => u.Clients)
            .HasForeignKey(c => c.CoachId)
            .OnDelete(DeleteBehavior.Restrict); // Don't delete clients if coach deleted

        // User → Notifications (one to many)
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Apply any extra configurations
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(FitPanelDbContext).Assembly);
    }

    public DbSet<Client> Clients { get; set; }
    public DbSet<WorkOut> WorkOuts { get; set; }
    public DbSet<WorkOutDay> WorkOutDays { get; set; }
    public DbSet<Excercise> Excercises { get; set; }
    public DbSet<Diet> Diets { get; set; }
    public DbSet<DietMeal> DietMeals { get; set; }
    public DbSet<MealItem> MealItems { get; set; }
    public DbSet<Cardio> Cardios { get; set; }

    public DbSet<AlternativeItem> AlternativeItems { get; set; }
    
    public DbSet<CoachExerciseDictionary> CoachExerciseDictionaries { get; set; }
    public DbSet<CoachMealDictionary> CoachMealDictionaries { get; set; }
    public DbSet<DietTemplate> DietTemplates { get; set; }
    public DbSet<WorkoutTemplate> WorkoutTemplates { get; set; }
    public DbSet<Notification> Notifications { get; set; }
}
