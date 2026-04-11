using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitPanel.Data.Configurations
{
    public class MealItemsConfigurations : IEntityTypeConfiguration<MealItem>
    {
        public void Configure(EntityTypeBuilder<MealItem> builder)
        {
            builder.HasKey(m => m.Id);

        builder.Property(m => m.MealName)
               .IsRequired()
               .HasMaxLength(150);

        builder.Property(m => m.Description)
               .IsRequired();

        builder.Property(m => m.Protein).IsRequired();
        builder.Property(m => m.Carbs).IsRequired();
        builder.Property(m => m.Fats).IsRequired();
        builder.Property(m => m.Calories).IsRequired();

        builder.HasMany(m => m.AlternativeItems)
               .WithOne(a => a.MealItem)
               .HasForeignKey(a => a.MealItemId)
               .OnDelete(DeleteBehavior.Cascade);
        }
    }
}