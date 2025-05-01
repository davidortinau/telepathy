using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Telepathic.Shared.Models
{
    public class CategoryTaskLoad
    {
        public string Title { get; set; } = string.Empty;
        public string Color { get; set; } = "#FF0000"; // Default color
        public int TaskCount { get; set; } = 0;

        public string DataLabelMappingName
        {
            get
            {
                return $"{Title}: {TaskCount.ToString()}";
            }
        }
    }
}
