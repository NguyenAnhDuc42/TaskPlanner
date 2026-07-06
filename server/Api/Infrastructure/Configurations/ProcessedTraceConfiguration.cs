using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain;

namespace Api;

public class ProcessedTraceConfiguration : IEntityTypeConfiguration<ProcessedTrace>
{
    public void Configure(EntityTypeBuilder<ProcessedTrace> builder)
    {
        builder.ToTable("processed_traces");

        builder.HasKey(x => x.TraceId);

        builder.Property(x => x.TraceId)
            .HasColumnName("trace_id")
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .HasColumnName("processed_at")
            .IsRequired();
    }
}
