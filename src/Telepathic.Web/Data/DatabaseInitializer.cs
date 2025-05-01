using Microsoft.EntityFrameworkCore;
using Telepathic.Web.Models;

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
            new Category { Title = "Product work", Color = "#FF0000" },
            new Category { Title = "Event prep", Color = "#00FF00" },
            new Category { Title = "Learning", Color = "#0000FF" }
        };

        await _context.Categories.AddRangeAsync(categories);
        await _context.SaveChangesAsync();

        // Add sample team members
        var teamMembers = new List<TeamMember>
        {
            new TeamMember 
            { 
                Name = "David Ortinau", 
                Role = "Product Manager",
                Email = "daortin@microsoft.com"
            },
            new TeamMember 
            { 
                Name = "Chris Hardy", 
                Role = "Team Lead",
                Email = "chhard@microsoft.com"
            },
             new TeamMember
            {
                Name = "Beth Massi",
                Role = "Product Manager",
                Email = "bethma@emicrosoft.com"
            },
             new TeamMember
            {
                Name = "Rachel Kang",
                Role = "Product Manager",
                Email = "rachelkang@emicrosoft.com"
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
                TeamMemberID = teamMembers[1].ID,
                ProjectTasks = new List<ProjectTask>
                {
                    new ProjectTask { Title = "Create project plan", DueDate=DateTime.Now.AddDays(20), IsCompleted = false, Priority = 1 },
                    new ProjectTask { Title = "Assign tasks", DueDate=DateTime.Now.AddDays(30), IsCompleted = false, Priority = 2 },
                    new ProjectTask { Title = "Coach new team members", DueDate=DateTime.Now.AddDays(25), IsCompleted = false, Priority = 2 }
                }
            },
             new Project
            {
                Name = "Prep for Build 2025",
                Description = "Build MAUI session content",
                Icon = "🏠",
                CategoryID = categories[1].ID,
                TeamMemberID = teamMembers[0].ID,
                ProjectTasks = new List<ProjectTask>
                {
                    new ProjectTask { Title = "Build out Telepathy sample", DueDate=DateTime.Now.AddDays(20), IsCompleted = false, Priority = 1 },
                    new ProjectTask { Title = "Prep slides for MAUI session", DueDate=DateTime.Now.AddDays(30), IsCompleted = false, Priority = 2 },
                    new ProjectTask { Title = "Coordinate content with presenters", DueDate=DateTime.Now.AddDays(25), IsCompleted = false, Priority = 2 }
                }
            },
            new Project
            {
                Name = "Prep for Build 2025",
                Description = "Build Blazor hybrid session content and hand-on labs",
                Icon = "🏠",
                CategoryID = categories[1].ID,
                TeamMemberID = teamMembers[2].ID,
                ProjectTasks = new List<ProjectTask>
                {
                    new ProjectTask { Title = "Build out lab 305", DueDate=DateTime.Now.AddDays(1), IsCompleted = true, Priority = 1 },
                    new ProjectTask { Title = "Prep demo for MAUI session", DueDate=DateTime.Now.AddDays(2), IsCompleted = true, Priority = 2 },
                    new ProjectTask { Title = "Polish slides for keynote", DueDate=DateTime.Now.AddDays(10), IsCompleted = false, Priority = 1 },
                    new ProjectTask { Title = "Coordinate content with presenters", DueDate=DateTime.Now.AddDays(5), IsCompleted = false, Priority = 2 },
                    new ProjectTask { Title = "Finalize content for Build", DueDate=DateTime.Now.AddDays(0), IsCompleted = true, Priority = 1 },
                    new ProjectTask { Title = "Conduct post-build review", DueDate=DateTime.Now.AddDays(45), IsCompleted = false, Priority = 3 },
                    new ProjectTask { Title = "Gather team feedback", DueDate=DateTime.Now.AddDays(50), IsCompleted = false, Priority = 3 }
                }
            },
             new Project
            {
                Name = "Customer success stories",
                Description = "Interviews and writeups of success stories with MAUI hybrid",
                Icon = "📋",
                CategoryID = categories[0].ID,
                TeamMemberID = teamMembers[2].ID,
                ProjectTasks = new List<ProjectTask>
                {
                    new ProjectTask { Title = "Interview with TT", DueDate=DateTime.Now.AddDays(5), IsCompleted = false, Priority = 1 },
                    new ProjectTask { Title = "Write up story", DueDate=DateTime.Now.AddDays(7), IsCompleted = false, Priority = 2 },
                    new ProjectTask { Title = "Review story", DueDate=DateTime.Now.AddDays(3), IsCompleted = false, Priority = 2 },
                    new ProjectTask { Title = "Publish story on website", DueDate=DateTime.Now.AddDays(10), IsCompleted = false, Priority = 2 }
                }
            },
            new Project
            {
                Name = "Learn Blazor",
                Description = "Study Blazor framework",
                Icon = "📚",
                CategoryID = categories[2].ID,
                TeamMemberID = teamMembers[3].ID,
                ProjectTasks = new List<ProjectTask>
                {
                    new ProjectTask { Title = "Complete tutorial", DueDate=DateTime.Now.AddDays(30), IsCompleted = false, Priority = 1 },
                    new ProjectTask { Title = "Build a sample app", DueDate=DateTime.Now.AddDays(45), IsCompleted = false, Priority = 2 },
                    new ProjectTask { Title = "Review Blazor concepts", DueDate=DateTime.Now.AddDays(60), IsCompleted = false, Priority = 3 },
                    new ProjectTask { Title = "Finalize Blazor project", DueDate=DateTime.Now.AddDays(90), IsCompleted = false, Priority = 4 },
                    new ProjectTask { Title = "Deploy Blazor application", DueDate=DateTime.Now.AddDays(120), IsCompleted = false, Priority = 5 },
                }
            }
        };

        await _context.Projects.AddRangeAsync(projects);
        await _context.SaveChangesAsync();
    }
}