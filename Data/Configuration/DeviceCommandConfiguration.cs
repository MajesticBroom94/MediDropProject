using MediDropProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediDropProject.Data.Configuration;

public sealed class DeviceCommandConfiguration
    : IEntityTypeConfiguration<DeviceCommand>
{
    public void Configure(EntityTypeBuilder<DeviceCommand> builder)
    {
        builder.ToTable("DeviceCommands");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnType("nvarchar(max)");
        
        builder.HasIndex(x => x.OperationId)
            .IsUnique();

        builder.HasIndex(x => new { x.DeviceId, x.CreatedAt });
        builder.HasIndex(x => new { x.DeviceId, x.Status });

        builder.HasOne(x => x.Device)
            .WithMany(x => x.Commands)
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RequestedByUser)
            .WithMany()
            .HasForeignKey(x => x.RequestedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}


