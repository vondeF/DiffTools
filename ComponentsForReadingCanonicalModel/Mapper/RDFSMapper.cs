using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace RDF_GOST_WORD.Mapper
{
    public static class RDFSMapper
    {
        static readonly List<NameSpace> Namespaces = new List<NameSpace>
        {
            new NameSpace("rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#"),
            new NameSpace("rdfs", "http://www.w3.org/2000/01/rdf-schema#"),
            new NameSpace("xsd", "http://www.w3.org/2001/XMLSchema#"),
            new NameSpace("xml", "http://www.w3.org/XML/1998/namespace"),
            new NameSpace("rf", "http://gost.ru/2019/schema-cim01#"),
            new NameSpace("so", "http://so-ups.ru/2015/schema-cim16#"),
            new NameSpace("cims", "http://iec.ch/TC57/1999/rdf-schema-extensions-19990926#"),
            new NameSpace("me", "http://monitel.com/2014/schema-cim16#"),
            new NameSpace("uml", "http://langdale.com.au/2005/UML#"),
            new NameSpace("cim", "http://iec.ch/TC57/CIM100#"),
            new NameSpace("ti", "https://www.ti-ees.ru/2025/schema-cim17#")
        };

        public static readonly Dictionary<string, string> Prefixes = Namespaces.ToDictionary(x => x.value.AbsoluteUri, x => x.key);

        public static Uri GetUriByStereotype(string type)
        {
            return Namespaces.First(x => x.key == type).value;
        } 
        public static List<NameSpace> GetNamespaces()
        {
            return Namespaces;
        }

        public static List<string> AllowedNamespaces = new List<string>()
        {
            "rf",
            "cim",
            "so",
            "ti"
        };

        public static string GetStereotype(XmlNode element, string classNamespace, bool excludeWrong = true)
        {
            var stereotype = element["UML:ModelElement.stereotype"];
            if (stereotype != null)
            {
                var value = stereotype["UML:Stereotype"];
                if (value != null)
                {
                    var val = value.Attributes["name"]?.Value;
                    if (val!=null && AllowedNamespaces.Contains(val))
                        return value.Attributes["name"]?.Value;
                }
            }
            return classNamespace;
        }
    }
}
