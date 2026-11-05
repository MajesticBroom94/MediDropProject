using MediDropProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediDropProject.Data.Configuration;

public sealed class CareProviderConfiguration
    : IEntityTypeConfiguration<CareProvider>
{
    public void Configure(EntityTypeBuilder<CareProvider> builder)
    {
        builder.ToTable("CareProviders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProfessionalTitle)
            .HasMaxLength(150);

        builder.Property(x => x.Bio)
            .HasMaxLength(4000);

        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.HasOne(x => x.User)
            .WithOne(x => x.CareProvider)
            .HasForeignKey<CareProvider>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
