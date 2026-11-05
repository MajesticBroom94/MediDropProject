using MediDropProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediDropProject.Data.Configuration;

public sealed class DeviceEventConfiguration
    : IEntityTypeConfiguration<DeviceEvent>
{
    public void Configure(EntityTypeBuilder<DeviceEvent> builder)
    {
        builder.ToTable("DeviceEvents");

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.EventId)
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Data)
            .HasColumnType("nvarchar(max)");
        
        builder.HasIndex(x => new { x.DeviceId, x.EventId })
            .IsUnique();
        
        builder.HasIndex(x => new { x.EventType, x.OccurredAt });
        builder.HasIndex(x => new { x.DeviceId, x.OperationId, x.OccurredAt });

        builder.HasOne(x => x.Device)
            .WithMany(x => x.Events)
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);
        
        
    }
}
