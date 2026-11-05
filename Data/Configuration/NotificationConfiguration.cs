using MediDropProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediDropProject.Data.Configuration;


public sealed class NotificationConfiguration
    : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.RecipientProfile)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Message)
            .HasMaxLength(4000)
            .IsRequired();
        
        builder.Property(x => x.DeduplicationKey).HasMaxLength(240);
        
        builder.HasIndex(x => new { x.UserId, x.DeduplicationKey })
            .IsUnique().HasFilter("[DeduplicationKey] IS NOT NULL");
        
        builder.HasOne(x => x.ApplicationUser)
            .WithMany(x => x.Notifications)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new
        {
            x.UserId,
            x.ReadAt,
            x.CreatedAt
        });
    }
}