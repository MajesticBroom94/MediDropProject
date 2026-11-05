using MediDropProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediDropProject.Data.Configuration;

public sealed class PatientMedicationConfiguration
    : IEntityTypeConfiguration<PatientMedication>
{
    public void Configure(EntityTypeBuilder<PatientMedication> builder)
    {
        builder.ToTable("PatientMedications");
        
        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasKey(x => x.Id);
        builder.ToTable(t => t.HasCheckConstraint("CK_PatientMedication_Quantity",
            "[Quantity] >= 0"));

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        
        builder.HasIndex(x => new { x.PatientId, x.Status });
        
        builder.HasIndex(x => new
            {
                x.PatientId,
                x.MedicationId
            })
            .IsUnique()
            .HasFilter("[Status] = 'Active'"); //todo: this needs to be verified
   

        builder.HasOne(x => x.Patient)
            .WithMany(x => x.Medications)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Medication)
            .WithMany(x => x.PatientMedications)
            .HasForeignKey(x => x.MedicationId)
            .OnDelete(DeleteBehavior.Restrict);
        
        
    }
}

