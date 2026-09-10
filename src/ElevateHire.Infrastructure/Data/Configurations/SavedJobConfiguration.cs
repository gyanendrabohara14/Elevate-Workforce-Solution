using ElevateHire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElevateHire.Infrastructure.Data.Configurations;

public class SavedJobConfiguration : IEntityTypeConfiguration<SavedJob>
{
    public void Configure(EntityTypeBuilder<SavedJob> builder)
    {
        builder.HasIndex(s => new { s.JobSeekerId, s.JobId }).IsUnique();

        builder.HasOne(s => s.Job)
            .WithMany(j => j.SavedJobs)
            .HasForeignKey(s => s.JobId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.JobSeeker)
            .WithMany(js => js.SavedJobs)
            .HasForeignKey(s => s.JobSeekerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}