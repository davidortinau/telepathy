using Microsoft.EntityFrameworkCore;
using Telepathic.Shared.Models;

namespace Telepathic.Web.Data;

public class TelepathicDbContext : DbContext
{
    public TelepathicDbContext(DbContextOptions<TelepathicDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects { get; set; } = default!;
    public DbSet<Category> Categories { get; set; } = default!;
    public DbSet<ProjectTask> ProjectTasks { get; set; } = default!;
    public DbSet<TeamMember> TeamMembers { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure relationships
        modelBuilder.Entity<Project>()
            .HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryID);

        modelBuilder.Entity<ProjectTask>()
            .HasOne<Project>()
            .WithMany(p => p.ProjectTasks)
            .HasForeignKey(t => t.ProjectID);

        // Configure TeamMember relationship
        modelBuilder.Entity<Project>()
            .HasOne(p => p.TeamMember)
            .WithMany(t => t.Projects)
            .HasForeignKey(p => p.TeamMemberID);
    }
}