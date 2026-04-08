using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace FitPanel.Data.Configurations;

    public class ClientConfigurations : IEntityTypeConfiguration<Client>
    {
        public void Configure(EntityTypeBuilder<Client> builder)
        {
           builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(c => c.Weight)
               .IsRequired();

        // 🔥 Relationship
        builder.HasMany(c => c.Diets)
               .WithOne(d => d.Client)
               .HasForeignKey(d => d.ClientId)
               .OnDelete(DeleteBehavior.Restrict); // or Restrict if you want

//one to many relationship between Client and WorkOuts
               builder.HasMany(c => c.WorkOuts)
                     .WithOne(w => w.Client)
                     .HasForeignKey(w => w.ClientId)
                     .OnDelete(DeleteBehavior.Restrict);
        }
    }
