using MediDropProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediDropProject.Data.Configuration;

public sealed class DeviceSlotConfiguration
    : IEntityTypeConfiguration<DeviceSlot>
{
    public void Configure(EntityTypeBuilder<DeviceSlot> builder)
    {
        builder.ToTable("DeviceSlots", table =>
        {
            table.HasCheckConstraint(
                "CK_DeviceSlot_SlotNumber",
                "[SlotNumber] > 0");
        });

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.DeviceId, x.SlotNumber })
            .IsUnique();

        // all slots belonging to device
        builder.HasOne(x => x.Device)
            .WithMany(x => x.Slots)
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasAlternateKey(x => new
        {
            x.DeviceId,
            x.Id
        });
    }
    
}
