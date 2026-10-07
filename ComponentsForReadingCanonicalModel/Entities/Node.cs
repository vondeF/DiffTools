using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RDF_GOST_WORD.Models
{
    public abstract class Node
    {
        public Dictionary<string, string> TaggedValues = new Dictionary<string, string>();
        public ObservableCollection<Node> ChildNodes { get; set; }
        protected XmlElement OwnedElements;
        public string Name { get; set; }
        public string Id { get; set; }
        public int? GostNumber { get; set; }
        public Guid? FootNote { get; set; }

        private void AddAttributesIfExist(XmlNode element)
        {
            var atrs = element["UML:ModelElement.taggedValue"];
            if (atrs != null)
            {
                foreach (XmlElement el in atrs)
                {
                    var tagName = el.Attributes["tag"]?.Value ?? "";
                    if (!string.IsNullOrEmpty(tagName) && !TaggedValues.ContainsKey(tagName))
                        TaggedValues.Add(tagName, el.Attributes["value"]?.Value);
                    else
                        Console.WriteLine($"{this.Name}: {tagName}");
                }
            }
        }

        private void AddChildNodeIfExist(XmlNode element)
        {
            var ownedElement = element["UML:Namespace.ownedElement"];
            if (ownedElement != null)
            {
                foreach (XmlNode el in element)
                {
                    var elements = element["UML:Diagram.element"];
                }
            }
        }

        protected Node(XmlNode element)
        {
            ChildNodes = new ObservableCollection<Node>();
            Name = element.Attributes["name"]?.Value;
            Id = element.Attributes["xmi.id"]?.Value;
            AddAttributesIfExist(element);
            AddChildNodeIfExist(element);
            XmlElement content = element?["UML:Namespace.ownedElement"];
            if (content != null)
                this.OwnedElements = content;
            if (TaggedValues.TryGetValue("GOST_R", out string val))
            {
                this.GostNumber = int.Parse(val.Replace("58651.", ""));
            }
        }
    }
}