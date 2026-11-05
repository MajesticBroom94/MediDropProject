using MediDropProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediDropProject.Data.Configuration;

public sealed class MedicationManufacturerConfiguration
    : IEntityTypeConfiguration<MedicationManufacturer>
{
    public void Configure(EntityTypeBuilder<MedicationManufacturer> builder)
    {
        builder.ToTable("MedicationManufacturers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();
        
        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasIndex(x => x.Name)
            .IsUnique();
    }
}
