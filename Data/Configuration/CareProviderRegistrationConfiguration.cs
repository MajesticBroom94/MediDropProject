using MediDropProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediDropProject.Data.Configuration;

public sealed class CareProviderRegistrationConfiguration
    : IEntityTypeConfiguration<CareProviderRegistration>
{
    public void Configure(EntityTypeBuilder<CareProviderRegistration> builder)
    {
        builder.ToTable("CareProviderRegistrations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RegistrationAuthority)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.RegistrationNumber)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ProviderType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.StatusReason).HasConversion<string>().HasMaxLength(60);

        builder.HasOne(x => x.Document)
            .WithMany()
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.SetNull);

        // Reapplications retain the same registration number in historical records.
        builder.HasIndex(x => new { x.CareProviderId, x.SubmittedAt });

        builder.HasOne(x => x.CareProvider)
            .WithMany(x => x.Registrations)
            .HasForeignKey(x => x.CareProviderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
