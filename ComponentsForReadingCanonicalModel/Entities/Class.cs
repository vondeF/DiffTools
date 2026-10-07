using RDF_GOST_WORD.Mapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using RDF_GOST_WORD.Services;
using System.Runtime.CompilerServices;

namespace RDF_GOST_WORD.Models
{
    public class Class : Node
    {
        public List<Attribute> Attributes = new List<Attribute>();
        public bool IsGost => this.stereotype == "rf";
        public bool IsUnitType => type == 1 || type == 2 || type == 4 || type == 5;
        public string stereotype { get; set; } = "cim";
        public bool isAbstract { get; set; }
        public string packageName { get; set; }
        public byte type { get; set; } = 3;
        public string additionalInfo { get; set; }
        public string basicProfileExtName { get; set; }

        public Uri GetUri()
        {
            var stereotype = RDFSMapper.GetUriByStereotype(this.stereotype);
            return new Uri(stereotype.AbsoluteUri + this.Name);
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
            if (stereotypes.Contains("so"))
                this.stereotype = "so";
            if (stereotypes.Contains("enumeration"))
                this.type = 1;
            else if (stereotypes.Contains("Compound"))
                this.type = 2;
            else if (stereotypes.Contains("CIMDatatype"))
                this.type = 4;
            else if (stereotypes.Contains("Primitive"))
                this.type = 5;
            else
                this.type = 3;
        }

        public string Description
        {
            get
            {
                if (this.TaggedValues.TryGetValue("documentation", out string val))
                {
                    return val;
                }
                return "";
            }
        }

        public string parentClassName { get; set; }
        public string parentClassStereotype { get; set; }

        public bool isObjectClass
        {
            get
            {
                return RDFSMapper.AllowedNamespaces.Contains(this.stereotype);
            }
        }

        public Class(XmlNode element) : base(element)
        {
            this.packageName = this.TaggedValues["package_name"] ?? "";
            if (bool.TryParse(element.Attributes["isAbstract"]?.Value, out var val))
            {
                isAbstract = val;
            }
            if (TaggedValues.TryGetValue("$ea_xref_property", out string value))
            {
                parseEaXrefProperty(value);
            }
            XmlNode features = element["UML:Classifier.feature"];
            if (features != null)
            {
                foreach (XmlNode el in features)
                {
                    Attributes.Add(new Attribute(el, this));
                }
            }
            if (this.type == 1)
            {
                this.additionalInfo = "Enumeration";
            }
            else if (this.type == 2)
            {
                this.additionalInfo = "Compound";
            }
            else if (this.type == 3)
            {
                // Обычный класс - без дополнительной информации
            }
            else if (this.type == 4)
            {
                this.additionalInfo = "CIMDatatype";
            }
            else if (this.type == 5)
            {
                this.additionalInfo = "Primitive";
            }
            else
            {
                var stereo = RDFSMapper.GetStereotype(element, this.stereotype);
                if (stereo != null && stereo != this.stereotype)
                {
                    this.additionalInfo = stereo;
                }
            }
        }

    }
}