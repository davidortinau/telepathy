namespace Telepathic.Web.Models;

public class ProjectTask
{
    public int ID { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime? DueDate { get; set; }
    public int Priority { get; set; }                       
    public int ProjectID { get; set; }              
    
}
