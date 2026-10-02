using ElevateWorkforce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElevateWorkforce.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(200);
        builder.HasIndex(u => u.Email).IsUnique();

        builder.HasOne(u => u.JobSeeker)
            .WithOne(js => js.User)
            .HasForeignKey<JobSeeker>(js => js.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.Employer)
            .WithOne(e => e.User)
            .HasForeignKey<Employer>(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}