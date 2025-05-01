using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telepathic.Shared.Models
{
    public class TasksDue
    {
        public DateTime? DueDate { get; set; } = DateTime.Now;
        public int TaskCount { get; set; } = 0;
        public int CompletedCount { get; set; } = 0;
        public string DueDateString => DueDate?.ToString("dd/MM/yyyy") ?? string.Empty;
    }

}
