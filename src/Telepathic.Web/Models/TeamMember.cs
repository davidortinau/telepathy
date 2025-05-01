namespace Telepathic.Web.Models;

public class TeamMember
{
    public int ID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public ICollection<Project> Projects { get; set; } = [];
}
