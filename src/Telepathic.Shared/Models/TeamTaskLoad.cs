using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telepathic.Shared.Models
{
    public class TeamTaskLoad
    {
        public string Name { get; set; } = string.Empty;
        public int TaskCount { get; set; } = 0;
        public string DataLabelMappingName
        {
            get {
                return $"{Name}: {TaskCount.ToString()}";
            }
        }
    }
}
