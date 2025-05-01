using Microsoft.EntityFrameworkCore;
using Telepathic.Shared.Models;

namespace Telepathic.Web.Data;

public class DatabaseInitializer
{
    private readonly TelepathicDbContext _context;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(TelepathicDbContext context, ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task InitializeDatabaseAsync()
    {
        try
        {
            // Ensure database is created and migrations applied
            await _context.Database.MigrateAsync();
            
            // Seed data if the database is empty
            if (!_context.Categories.Any())
            {
                await SeedDataAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initializing the database");
            throw;
        }
    }

    private async Task SeedDataAsync()
    {
        // Add sample categories
        var categories = new List<Category>
        {
            new Category { Title = "Work", Color = "#FF0000" },
            new Category { Title = "Personal", Color = "#00FF00" },
            new Category { Title = "Learning", Color = "#0000FF" }
        };

        await _context.Categories.AddRangeAsync(categories);
        await _context.SaveChangesAsync();

        // Add sample team members
        var teamMembers = new List<TeamMember>
        {
            new TeamMember 
            { 
                Name = "John Doe", 
                Role = "Project Manager",
                Email = "john.doe@example.com"
            },
            new TeamMember 
            { 
                Name = "Jane Smith", 
                Role = "Team Lead",
                Email = "jane.smith@example.com"
            }
        };

        await _context.TeamMembers.AddRangeAsync(teamMembers);
        await _context.SaveChangesAsync();

        // Add sample projects
        var projects = new List<Project>
        {
            new Project
            {
                Name = "Project Management",
                Description = "Manage various projects",
                Icon = "📋",
                CategoryID = categories[0].ID,
                TeamMemberID = teamMembers[0].ID,
                ProjectTasks = new List<ProjectTask>
                {
                    new ProjectTask { Title = "Create project plan", IsCompleted = false, Priority = 1 },
                    new ProjectTask { Title = "Assign tasks", IsCompleted = false, Priority = 2 }
                }
            },
            new Project
            {
                Name = "Home Improvement",
                Description = "Tasks around the house",
                Icon = "🏠",
                CategoryID = categories[1].ID,
                TeamMemberID = teamMembers[0].ID,
                ProjectTasks = new List<ProjectTask>
                {
                    new ProjectTask { Title = "Fix the faucet", IsCompleted = false, Priority = 1 },
                    new ProjectTask { Title = "Paint the bedroom", IsCompleted = false, Priority = 2 }
                }
            },
            new Project
            {
                Name = "Learn Blazor",
                Description = "Study Blazor framework",
                Icon = "📚",
                CategoryID = categories[2].ID,
                TeamMemberID = teamMembers[1].ID,
                ProjectTasks = new List<ProjectTask>
                {
                    new ProjectTask { Title = "Complete tutorial", IsCompleted = false, Priority = 1 },
                    new ProjectTask { Title = "Build a sample app", IsCompleted = false, Priority = 2 }
                }
            }
        };

        await _context.Projects.AddRangeAsync(projects);
        await _context.SaveChangesAsync();
    }
}