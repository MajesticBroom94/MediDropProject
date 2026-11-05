using MediDropProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediDropProject.Data.Configuration;

public sealed class MedicationRoundConfiguration : IEntityTypeConfiguration<MedicationRound>
{
    public void Configure(EntityTypeBuilder<MedicationRound> builder)
    {
        builder.ToTable("MedicationRounds");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(x => new { x.PatientId, x.CreatedAt });
        builder.HasIndex(x => new { x.DeviceId, x.CreatedAt });
        builder.HasIndex(x => x.DeviceId).IsUnique()
            .HasFilter("[Status] = 'Active'");
        builder.HasIndex(x => new { x.Status, x.CreatedAt });

        builder.HasOne(x => x.Patient)
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Device)
            .WithMany()
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Slots)
            .WithOne(x => x.MedicationRound)
            .HasForeignKey(x => x.MedicationRoundId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
