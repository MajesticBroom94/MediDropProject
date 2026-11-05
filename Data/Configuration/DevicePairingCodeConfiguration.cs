using MediDropProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediDropProject.Data.Configuration;

public sealed class DevicePairingCodeConfiguration
    : IEntityTypeConfiguration<DevicePairingCode>
{
    public void Configure(EntityTypeBuilder<DevicePairingCode> builder)
    {
        builder.ToTable("DevicePairingCodes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CodeHash)
            .HasMaxLength(64)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.ProtectedCode)
            .HasMaxLength(1000)
            .IsRequired();

        builder.HasIndex(x => x.CodeHash)
            .IsUnique();
        
        // At most one unused pairing code can exist for a device.
        builder.HasIndex(x => x.DeviceId)
            .IsUnique()
            .HasFilter("[InvalidatedAt] IS NULL");

        builder.HasOne(x => x.Device)
            .WithMany(x => x.PairingCodes)
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
