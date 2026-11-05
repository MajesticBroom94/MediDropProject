using MediDropProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediDropProject.Data.Configuration;

public sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.HasOne(x => x.User)
            .WithOne(x => x.Patient)
            .HasForeignKey<Patient>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
    }
}
