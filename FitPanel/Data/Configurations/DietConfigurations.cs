using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitPanel.Data.Configurations
{
    public class DietConfigurations : IEntityTypeConfiguration<Diet>
    {
        public void Configure(EntityTypeBuilder<Diet> builder)
        {
             builder.HasKey(d => d.Id);

        builder.Property(d => d.NumberOfMeals)
               .IsRequired();

        builder.Property(d => d.CreatedAt)
               .IsRequired();

       

                 // 🔥 RELATIONSHIP WITH MEAL ITEMS
        builder.HasMany(d => d.MealItems)
               .WithOne(m => m.Diet)
               .HasForeignKey(m => m.DietId)
               .OnDelete(DeleteBehavior.Cascade);
        }
    }
}