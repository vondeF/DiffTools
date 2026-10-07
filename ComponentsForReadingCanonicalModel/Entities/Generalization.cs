using RDF_GOST_WORD.Mapper;
using System;
using System.Linq;
using System.Xml;

namespace RDF_GOST_WORD.Models
{
    public class Generalization : Node
    {
        public string subtype { get; set; }
        public Guid subtypeUid { get; set; }
        public string supertype { get; set; }
        public Guid supertypeUid { get; set; }
        public string target { get; set; }
        public string source { get; set; }
        public string stereotype { get; set; }

        public bool IsGost => this.stereotype == "rf";
        public Generalization(XmlNode element) : base(element)
        {
            this.subtype = element.Attributes["subtype"]?.Value;
            //this.subtypeUid = element.Attributes[""]
            this.supertype = element.Attributes["supertype"]?.Value;
            this.target = this.TaggedValues["ea_targetName"] ?? "";
            this.source = this.TaggedValues["ea_sourceName"] ?? "";
            this.stereotype = RDFSMapper.GetStereotype(element, "cim");
        }
    }
}
