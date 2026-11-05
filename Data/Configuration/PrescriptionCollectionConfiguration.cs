using MediDropProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediDropProject.Data.Configuration;

public sealed class PrescriptionCollectionConfiguration : IEntityTypeConfiguration<PrescriptionCollection>
{
    public void Configure(EntityTypeBuilder<PrescriptionCollection> builder)
    {
        builder.ToTable("PrescriptionCollections");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.PrescriptionId, x.FillNumber }).IsUnique();
        builder.HasOne(x => x.Prescription).WithMany(x => x.Collections)
            .HasForeignKey(x => x.PrescriptionId).OnDelete(DeleteBehavior.Cascade);
    }
}
