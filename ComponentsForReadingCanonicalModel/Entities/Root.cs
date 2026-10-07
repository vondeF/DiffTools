using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RDF_GOST_WORD.Models
{
    public class Root: Node
    {
        public List<Node> Nodes = new List<Node>();
        public Model Model;
        public List<RootGostTaggedValue> GostTaggedValues = new List<RootGostTaggedValue>();
        public Dictionary<Guid, ClassifierRole> FootnoteClassifierRoles = new Dictionary<Guid, ClassifierRole>();        
        public Root(XmlNode element) : base(element)
        {
            
        }
    }
}
