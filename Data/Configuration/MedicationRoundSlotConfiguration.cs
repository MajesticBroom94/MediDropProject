using MediDropProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediDropProject.Data.Configuration;

public sealed class MedicationRoundSlotConfiguration
    : IEntityTypeConfiguration<MedicationRoundSlot>
{
    public void Configure(EntityTypeBuilder<MedicationRoundSlot> builder)
    {
        builder.ToTable("MedicationRoundSlots");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.MedicationRoundId,
            x.ScheduledFor
        });

        builder.HasIndex(x => new
        {
            x.DeviceSlotId,
            x.ScheduledFor
        });

        builder.HasIndex(x => new
        {
            x.Status,
            x.ScheduledFor
        });

        builder.HasOne(x => x.MedicationRound)
            .WithMany(x => x.Slots)
            .HasForeignKey(x => x.MedicationRoundId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DeviceSlot)
            .WithMany(x => x.MedicationRoundSlots)
            .HasForeignKey(x => x.DeviceSlotId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(x => x.OperationId)
            .IsUnique();
    }
}
