using MediDropProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediDropProject.Data.Configuration;

public sealed class UserSettingConfiguration : IEntityTypeConfiguration<UserSetting>
{
    public void Configure(EntityTypeBuilder<UserSetting> builder)
    {
        builder.ToTable("UserSettings", table =>
        {
            table.HasCheckConstraint(
                "CK_UserSetting_ReminderLeadMinutes",
                "[ReminderLeadMinutes] >= 5 AND [ReminderLeadMinutes] <= 120");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.Property(x => x.ReminderLeadMinutes)
            .HasDefaultValue(15)
            .IsRequired();

        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.HasOne(x => x.User)
            .WithOne(x => x.Settings)
            .HasForeignKey<UserSetting>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}