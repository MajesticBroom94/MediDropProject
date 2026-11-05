using MediDropProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediDropProject.Data.Configuration;

public sealed class PatientCareRelationshipConfiguration
    : IEntityTypeConfiguration<PatientCareRelationship>
{
    public void Configure(EntityTypeBuilder<PatientCareRelationship> builder)
    {
        builder.ToTable("PatientCareRelationships");

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.PatientId,
            x.CareProviderId,
            x.Status
        });

        builder.HasOne(x => x.Patient)
            .WithMany(x => x.CareRelationships)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CareProvider)
            .WithMany(x => x.PatientRelationships)
            .HasForeignKey(x => x.CareProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.GrantedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

