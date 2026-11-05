using MediDropProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediDropProject.Data.Configuration;

public sealed class DeviceCredentialConfiguration
    : IEntityTypeConfiguration<DeviceCredential>
{
    public void Configure(EntityTypeBuilder<DeviceCredential> builder)
    {
        builder.ToTable("DeviceCredentials");

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.CredentialHash)
            .HasMaxLength(64)
            .IsUnicode(false)
            .IsRequired();

        builder.HasIndex(x => x.CredentialHash)
            .IsUnique();
        
        builder.HasIndex(x => x.DeviceId);
        
    }
}

