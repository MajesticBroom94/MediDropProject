using MediDropProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediDropProject.Data.Configuration;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.StorageKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex(x => x.StorageKey)
            .IsUnique();

        builder.HasIndex(x => x.CreatedAt);

        builder.HasOne(x => x.UploadedByUser)
            .WithMany()
            .HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Patient>().WithMany().HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
