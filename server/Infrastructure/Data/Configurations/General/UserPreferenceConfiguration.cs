using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class UserPreferenceConfiguration : EntityConfiguration<UserPreference>
{
    public override void Configure(EntityTypeBuilder<UserPreference> builder)
    {
        base.Configure(builder);

        builder.ToTable("user_preferences");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.Setting)
            .HasColumnName("setting")
            .HasColumnType("jsonb")
            .IsRequired();
        
        builder.HasIndex(x => x.UserId)
            .IsUnique();
    }
}
