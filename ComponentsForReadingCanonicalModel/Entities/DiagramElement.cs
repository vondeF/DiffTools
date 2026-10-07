using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RDF_GOST_WORD.Models
{
    public class DiagramElement
    {
        public string geometry { get; set; }
        public string subject { get; set; }
        public int? seqno { get; set; }

        public DiagramElement(XmlElement DiagramElement)
        {
            this.geometry = DiagramElement.Attributes["geometry"]?.Value;
            this.subject = DiagramElement.Attributes["subject"]?.Value;
            if (int.TryParse(DiagramElement.Attributes["seqno"]?.Value, out var s))
                this.seqno = s;
        }
    }
}
