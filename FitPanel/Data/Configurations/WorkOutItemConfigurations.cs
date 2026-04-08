using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitPanel.Data.Configurations
{
    public class WorkOutItemConfigurations : IEntityTypeConfiguration<Excercise>
    {
        public void Configure(EntityTypeBuilder<Excercise> builder)
        {
           builder.HasKey(d => d.Id);

        builder.Property(d => d.ExerciseName)
               .IsRequired()
               .HasMaxLength(255);

        builder.Property(d => d.RestTime)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(d => d.Reps)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(d => d.Sets)
               .IsRequired()
               .HasMaxLength(50);

               }
    }
}