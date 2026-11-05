using MediDropProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediDropProject.Data.Configuration;

public sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("AuditEvents");

        builder.HasKey(x => x.Id);


        builder.Property(x => x.EventType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.EntityType)
            .HasMaxLength(100);

        builder.Property(x => x.EntityId)
            .HasMaxLength(100);


        builder.HasIndex(x => x.OccurredAt);

        builder.HasIndex(x => new { x.ActorUserId, x.OccurredAt });


        builder.HasOne(x => x.ActorUser)
            .WithMany(x => x.AuditEvents)
            .HasForeignKey(x => x.ActorUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
