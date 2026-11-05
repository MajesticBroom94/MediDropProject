using MediDropProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediDropProject.Data.Configuration;

public sealed class SecurityEventConfiguration
    : IEntityTypeConfiguration<SecurityEvent>
{
    public void Configure(EntityTypeBuilder<SecurityEvent> builder)
    {
        builder.ToTable("SecurityEvents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.FailureReason)
            .HasMaxLength(1000);

        builder.Property(x => x.IpAddress)
            .HasMaxLength(45);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(1000);

        builder.Property(x => x.Metadata)
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(x => x.OccurredAt);
        builder.HasIndex(x => new { x.UserId, x.OccurredAt });
        builder.HasIndex(x => new { x.Type, x.OccurredAt });

        builder.HasOne(x => x.User)
            .WithMany(x => x.SecurityEvents)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

