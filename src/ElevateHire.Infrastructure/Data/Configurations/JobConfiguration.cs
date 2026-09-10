using ElevateHire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElevateHire.Infrastructure.Data.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.Property(j => j.Title).IsRequired().HasMaxLength(200);
        builder.HasIndex(j => j.Status);
        builder.HasIndex(j => new { j.Status, j.PublishedAt });
        builder.HasIndex(j => j.Title);
        builder.HasIndex(j => j.Location);
        builder.HasIndex(j => j.Category);

        builder.HasOne(j => j.Company)
            .WithMany(c => c.Jobs)
            .HasForeignKey(j => j.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}