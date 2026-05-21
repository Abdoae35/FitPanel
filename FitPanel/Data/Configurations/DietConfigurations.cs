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

       

        // 🔥 RELATIONSHIP WITH DIET MEALS
        builder.HasMany(d => d.DietMeals)
               .WithOne(dm => dm.Diet)
               .HasForeignKey(dm => dm.DietId)
               .OnDelete(DeleteBehavior.Cascade);
        }
    }
}