using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitPanel.Data.Configurations
{
    public class WorkOutDayConfigurations : IEntityTypeConfiguration<WorkOutDay>
    {
        public void Configure(EntityTypeBuilder<WorkOutDay> builder)
        {
            builder.HasKey(d => d.Id);

        builder.Property(d => d.DayName)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(d => d.Day)
               .IsRequired();
        // 🔥 Relationship: WorkOutDay → WorkOutItem
        builder.HasMany(d => d.ExcerciseItems)
               .WithOne(i => i.WorkOutDay)
               .HasForeignKey(i => i.WorkOutDayId)
               .OnDelete(DeleteBehavior.Cascade);


                builder.HasOne(d => d.Cardio)
    .WithOne(c => c.WorkOutDay)
    .HasForeignKey<Cardio>(c => c.WorkOutDayId)
    .OnDelete(DeleteBehavior.Cascade);

        }
    }
}