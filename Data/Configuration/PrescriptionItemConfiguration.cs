using MediDropProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicationDispenserProject.Data.Configuration;

public sealed class PrescriptionItemConfiguration
    : IEntityTypeConfiguration<PrescriptionItem>
{
    public void Configure(EntityTypeBuilder<PrescriptionItem> builder)
    {
        builder.ToTable("PrescriptionItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DosageAmount)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.DosageUnit)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Frequency)
            .HasMaxLength(200);

        builder.Property(x => x.Instructions)
            .HasMaxLength(4000);

        builder.HasOne(x => x.Prescription)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Medication)
            .WithMany(x => x.PrescriptionItems)
            .HasForeignKey(x => x.MedicationId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(x => x.PatientMedication)
            .WithMany(x => x.PrescriptionItems)
            .HasForeignKey(x => x.PatientMedicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.PatientMedicationId,
            x.PrescriptionId
        });


        builder.HasIndex(x => new
        {
            x.PrescriptionId,
            x.MedicationId
        });
    }
}
