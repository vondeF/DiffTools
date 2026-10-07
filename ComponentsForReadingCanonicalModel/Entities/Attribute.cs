using RDF_GOST_WORD.Mapper;
using System;
using System.Collections.Generic;
using System.Xml;
using RDF_GOST_WORD.Utils;


namespace RDF_GOST_WORD.Models
{
    public class Attribute : Node
    {
        public string stereotype { get; set; }
        public bool IsGost => this.stereotype == "rf";
        private string className;
        private string classStereotype;
        public Guid? classRef = null;
        public Uri dataTypeUri;
        public string typeClassName {get; private set;}
        public string typeOfTypeName { get; set; }
        public string classifierString { get; set; }
        public string defaultValue { get; set; }
        public bool IsRequired { get; private set; } = false;
        public readonly Class _class;

        public string Description
        {
            get
            {
                if (this.TaggedValues.TryGetValue("description", out string val))
                    return val;
                return "";
            }
        }
        public Attribute(XmlNode element, Class _class) : base(element)
        {
            this.stereotype = RDFSMapper.GetStereotype(element, _class.stereotype);
            this.className = _class.Name;
            this.defaultValue = null;
            this.classStereotype = _class.stereotype;
            if (element["UML:Attribute.initialValue"] != null && element["UML:Attribute.initialValue"]["UML:Expression"] != null)
            {
                var headElement = element["UML:Attribute.initialValue"]["UML:Expression"];
                if (headElement != null && headElement.Attributes.Count > 0) 
                    defaultValue = element["UML:Attribute.initialValue"]["UML:Expression"].Attributes[0].Value;
            }
            if (TaggedValues.TryGetValue("type", out string _type))
            {
                this.typeClassName = _type;
            }
            else
            {
                this.typeClassName = _class.Name;
            }
            if (TaggedValues.TryGetValue("lowerBound", out var lowerBound))
            {
                IsRequired = lowerBound == "1";
            }

            this._class = _class;
            classifierString = element?["UML:StructuralFeature.type"]?["UML:Classifier"]?.Attributes["xmi.idref"].Value;
            classRef = classifierString.ToGuidFromEAID();
            /*
            try
            {
                this.classRef = new Guid(classifierString.Replace("EAID_", "").Replace("_", "-"));
            }
            catch (Exception e)
            {

            }
            */
        }
    }
}
