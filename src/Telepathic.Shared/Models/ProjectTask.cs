using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Telepathic.Shared.Models
{
    public class ProjectTask
    {
        public int ID { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTime? DueDate { get; set; }
        public int Priority { get; set; }                       
        public int ProjectID { get; set; }              
        
    }
}
