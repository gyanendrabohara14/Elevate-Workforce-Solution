using ElevateHire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElevateHire.Infrastructure.Data.Configurations;

public class JobSeekerConfiguration : IEntityTypeConfiguration<JobSeeker>
{
    public void Configure(EntityTypeBuilder<JobSeeker> builder)
    {
        builder.HasIndex(js => js.UserId).IsUnique();

        builder.HasOne(js => js.User)
            .WithOne(u => u.JobSeeker)
            .HasForeignKey<JobSeeker>(js => js.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(js => js.Applications)
            .WithOne(a => a.JobSeeker)
            .HasForeignKey(a => a.JobSeekerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(js => js.SavedJobs)
            .WithOne(s => s.JobSeeker)
            .HasForeignKey(s => s.JobSeekerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(js => js.Notifications)
            .WithOne(n => n.JobSeeker)
            .HasForeignKey(n => n.JobSeekerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}