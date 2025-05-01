using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Telepathic.Shared.Models
{
    public class Category
    {
        public int ID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Color { get; set; } = "#FF0000";        
    }
}
