using MediDropProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediDropProject.Data.Configuration;

public sealed class PrescriptionConfiguration
    : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable("Prescriptions");
        
        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PrescriptionNumber)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Instructions)
            .HasMaxLength(4000);

        builder.Property(x => x.Notes)
            .HasMaxLength(4000);
        builder.Property(x => x.CollectedCount).HasDefaultValue(0);

        builder.HasIndex(x => x.PrescriptionNumber)
            .IsUnique();

        builder.HasIndex(x => new { x.PatientId, x.Status });

        builder.HasOne(x => x.Patient)
            .WithMany(x => x.Prescriptions)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PrescribedBy)
            .WithMany()
            .HasForeignKey(x => x.PrescribedById)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(x => x.Document)
            .WithOne()
            .HasForeignKey<Prescription>(x => x.DocumentId)
            .OnDelete(DeleteBehavior.SetNull);

    }
}

