using MediDropProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediDropProject.Data.Configuration;

public sealed class MedicationConfiguration : IEntityTypeConfiguration<Medication>
{
    public void Configure(EntityTypeBuilder<Medication> builder)
    {
        builder.ToTable("Medications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.GenericName)
            .HasMaxLength(200);

        builder.Property(x => x.Strength)
            .HasMaxLength(100);

        builder.Property(x => x.Form)
            .HasMaxLength(100);

        builder.Property(x => x.Barcode)
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(4000);

        builder.Property(x => x.ActiveIngredients).HasMaxLength(1000);
        builder.Property(x => x.SafetyInformation).HasMaxLength(4000);

        builder.Property(x => x.ImageUrl)
            .HasMaxLength(1000);

        builder.HasIndex(x => x.Barcode)
            .IsUnique()
            .HasFilter("[Barcode] IS NOT NULL");

        builder.HasIndex(x => new
        {
            x.Name,
            x.Strength,
            x.Form,
            x.ManufacturerId
        });

        builder.HasOne(x => x.Manufacturer)
            .WithMany(x => x.Medications)
            .HasForeignKey(x => x.ManufacturerId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        
        builder.HasIndex(x => x.Status);
        
        builder.HasOne(x => x.SubmittedByUser)
            .WithMany()
            .HasForeignKey(x => x.SubmittedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

