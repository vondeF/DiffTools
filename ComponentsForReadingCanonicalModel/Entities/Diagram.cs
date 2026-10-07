using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RDF_GOST_WORD.Models
{
    public class Diagram: Node
    {
        public string diagramType { get; set; }
        public List<DiagramElement> diagramElements = new List<DiagramElement>();
        public Diagram (XmlNode element) : base(element)
        {
            var elements = element["UML:Diagram.element"];
            if (elements != null)
            {
                foreach (XmlElement el in elements)
                {
                    diagramElements.Add(new DiagramElement(el));
                }
            }
            //AddAttributesIfExist(Diagram);
        }
    }
}
