using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Telepathic.Shared.Models
{
    public class Project
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int CategoryID { get; set; }
        public Category? Category { get; set; }
        public ICollection<ProjectTask> ProjectTasks { get; set; } = [];
        public int? TeamMemberID { get; set; }
        public TeamMember? TeamMember { get; set; }
    }
}
