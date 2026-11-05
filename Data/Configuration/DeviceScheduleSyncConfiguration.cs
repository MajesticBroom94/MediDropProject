using MediDropProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediDropProject.Data.Configuration;

public sealed class DeviceScheduleSyncConfiguration : IEntityTypeConfiguration<DeviceScheduleSync>
{
    public void Configure(EntityTypeBuilder<DeviceScheduleSync> builder)
    {
        builder.ToTable("DeviceScheduleSyncs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ScheduleVersion)
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => new
            {
                x.DeviceId,
                x.MedicationRoundId,
                x.ScheduleVersion
            })
            .IsUnique();

        builder.HasIndex(x => new
        {
            x.DeviceId,
            x.CreatedAt
        });

        builder.HasOne(x => x.Device)
            .WithMany(x => x.ScheduleSyncs)
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.MedicationRound)
            .WithMany(x => x.ScheduleSyncs)
            .HasForeignKey(x => x.MedicationRoundId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
