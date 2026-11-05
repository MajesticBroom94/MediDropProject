using MediDropProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediDropProject.Data.Configuration;

public sealed class DeviceAssignmentConfiguration
    : IEntityTypeConfiguration<DeviceAssignment>
{
    public void Configure(EntityTypeBuilder<DeviceAssignment> builder)
    {
        builder.ToTable("DeviceAssignments");

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(x => new { x.DeviceId, x.Status });
        // One physical dispenser cannot have two owners.
        builder.HasIndex(x => x.DeviceId).IsUnique()
            .HasFilter("[UnassignedAt] IS NULL");
        builder.HasIndex(x => new { x.PatientId, x.Status });
        builder.HasIndex(x => x.AssignedAt);

        builder.HasOne(x => x.Device)
            .WithMany(x => x.Assignments)
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Patient)
            .WithMany(x => x.DeviceAssignments)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
