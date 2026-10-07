using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RDF_GOST_WORD.Models
{
    public class AssociationEnd : Node
    {
        public string multiplicity { get; set; }
        public string type { get; set; }
        public string aggregation { get; set; }
        public bool isSource { get; set; }
        public string stereotype { get; set; }
        public AssociationEnd(XmlNode element) : base(element)
        {
            this.multiplicity = element.Attributes["multiplicity"]?.Value ?? "0..*";
            this.type = element.Attributes["type"]?.Value;
            this.aggregation = element.Attributes["aggregation"]?.Value;
            this.isSource = this.TaggedValues["ea_end"] == "source";
        }

        public string GetDescription()
        {
            if (this.TaggedValues.TryGetValue("description", out string description))
                return description;
            return "";
        }
    }
}



