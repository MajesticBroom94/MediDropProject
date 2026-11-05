using MediDropProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediDropProject.Data.Configuration;

public sealed class MedicationRoundSlotItemConfiguration : IEntityTypeConfiguration<MedicationRoundSlotItem>
{
    public void Configure(EntityTypeBuilder<MedicationRoundSlotItem> builder)
    {
        builder.ToTable("MedicationRoundSlotItems");

        builder.HasKey(x => x.Id);
        builder.ToTable(t => t.HasCheckConstraint("CK_MedicationRoundSlotItem_Quantity",
            "[Quantity] > 0"));

        builder.Property(x => x.Quantity)
            .IsRequired();

        builder.Property(x => x.MedicationName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.MedicationStrength)
            .HasMaxLength(100);

        builder.Property(x => x.MedicationForm)
            .HasMaxLength(100);

        builder.HasIndex(x => new
        {
            x.MedicationRoundSlotId,
            x.PatientMedicationId
        });

        builder.HasOne(x => x.MedicationRoundSlot)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.MedicationRoundSlotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PatientMedication)
            .WithMany()
            .HasForeignKey(x => x.PatientMedicationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
