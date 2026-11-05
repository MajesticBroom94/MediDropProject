using MediDropProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediDropProject.Data.Configuration;

public sealed class DispenserDeviceConfiguration
    : IEntityTypeConfiguration<DispenserDevice>
{
    public void Configure(EntityTypeBuilder<DispenserDevice> builder)
    {
        builder.ToTable("DispenserDevices");
        
        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DeviceUid)
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(x => x.Name)
            .HasMaxLength(150);
        
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        
        builder.Property(x => x.OperatingState)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        
        builder
            .HasMany(x => x.Slots)
            .WithOne(x => x.Device)
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.
             HasOne(d => d.Credential)
            .WithOne(c => c.Device)
            .HasForeignKey<DeviceCredential>(c => c.DeviceId)
             .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(x => x.DeviceUid)
            .IsUnique();
    }
}
