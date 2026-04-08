using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitPanel.Data.Configurations
{
    public class WorkOutConfigurations : IEntityTypeConfiguration<WorkOut>
    {
        public void Configure(EntityTypeBuilder<WorkOut> builder)
        {
            builder.HasKey(w => w.Id);

        builder.Property(w => w.SplitName)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(w => w.NumberOfWorkOutDays)
               .IsRequired();

        builder.Property(w => w.CreatedAt)
               .IsRequired();


        // 🔥 Relationship: WorkOut → WorkOutDay
        builder.HasMany(w => w.WorkOutDays)
               .WithOne(d => d.WorkOut)
               .HasForeignKey(d => d.WorkOutId)
               .OnDelete(DeleteBehavior.Cascade);
        }
    }
}