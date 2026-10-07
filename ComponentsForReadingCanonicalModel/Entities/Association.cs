using RDF_GOST_WORD.Mapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace RDF_GOST_WORD.Models
{
    public class Association : Node
    {
        public bool isAbstract { get; set; }
        public List<AssociationEnd> AssociationEnds { get; set; }
        public string source { get; set; }
        public string target { get; set; }
        public string stereotype { get; set; }
        public bool IsGost => this.stereotype == "rf";
        public Association(XmlNode element) : base(element)
        {
            AssociationEnds = new List<AssociationEnd>();
            if (bool.TryParse(element.Attributes["isAbstract"]?.Value, out var val)) {
                isAbstract = val;
            }
            var elements = element["UML:Association.connection"];
            if (elements != null)
            {
                foreach (XmlElement el in elements)
                {
                    var asEnd = new AssociationEnd(el);
                    AssociationEnds.Add(asEnd);
                }
            }
            this.source = this.TaggedValues["ea_sourceName"] ?? "";
            this.target = this.TaggedValues["ea_targetName"] ?? "";
            if (TaggedValues.TryGetValue("$ea_xref_property", out string value))
            {
                parseEaXrefProperty(value);
            }
            this.stereotype = RDFSMapper.GetStereotype(element, "cim");
        }

      
        private void parseEaXrefProperty(string tagValue)
        {
            List<string> stereotypes = new List<string>();
            string pattern = @"\$DES=([^$]+)\$DES;";
            var matches = Regex.Matches(tagValue, pattern);
            foreach (Match match in matches)
            {
                string val = match.Groups[1].Value.Trim();
                var nameMatch = Regex.Matches(val, @"Name=([^;]+);");
                foreach (Match m in nameMatch)
                {
                    stereotypes.Add(m.Groups[1].Value.Trim());
                }
            }
            if (stereotypes.Contains("rf"))
                this.stereotype = "rf";
        }

        public string DirectAssociationName
        {
            get
            {
                return $"{this.source}.{GetTargetAssociationEnd().Name}";
            }
        }

        public string ReverseAssociationName
        {
            get
            {
                return $"{this.target}.{GetSourceAssociationEnd().Name}";
            }
        }

        public AssociationEnd GetTargetAssociationEnd()
        {
            return AssociationEnds.First(x => !x.isSource);
        }

        public AssociationEnd GetSourceAssociationEnd()
        {
            return AssociationEnds.First(x => x.isSource);
        }

        /// <summary>
        /// Получить название ассоциации относительно указанного класса
        /// </summary>
        public string GetLabel(string sourceClassName)
        {
            if (source == sourceClassName)
            {
                return GetTargetAssociationEnd().Name;
            }
            else
            {
                return GetSourceAssociationEnd().Name;
            }
        }

        private string GetDirectAssociationName()
        {
            var directAssociationNameArray = this.DirectAssociationName.Split('.');
            return directAssociationNameArray.Last();
        }

        private string GetReverseAssociationName()
        {
            var reverseAssociationNameArray = this.ReverseAssociationName.Split('.');
            return reverseAssociationNameArray.Last();
        }
    }
}


