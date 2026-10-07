using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RDF_GOST_WORD.Models
{
    public class Package : Node
    {   
        public List<Package> childPackages { get; set; }
        public List<Class> classes { get; set; }
        public List<Generalization> generalizations { get; set; }
        public List<Association> associations { get; set; }
        public List<Collaboration> collaborations { get; set; }
        public Package(XmlNode element) : base(element)
        {
            if (OwnedElements != null)
            {
                childPackages = new List<Package>();
                classes = new List<Class>();
                generalizations = new List<Generalization>();
                associations = new List<Association>();
                collaborations = new List<Collaboration>();
                foreach (XmlElement xnode in OwnedElements)
                {
                    try
                    {
                        var localName = xnode.LocalName;
                        if (localName == "Package")
                        {
                            childPackages.Add(new Package(xnode));
                        }
                        else if (localName == "Class")
                        {
                            classes.Add(new Class(xnode));
                        }
                        else if (localName == "Generalization")
                        {
                            generalizations.Add(new Generalization(xnode));
                        }
                        else if (localName == "Association")
                        {
                            var assoc = new Association(xnode);
                            if (!assoc.AssociationEnds.Any(x => x.Name == null))
                                associations.Add(new Association(xnode));
                        }
                        else if (localName == "Collaboration")
                        {
                            collaborations.Add(new Collaboration(xnode));
                        }
                    }
                    catch (Exception e)
                    {
                        
                    }
                }
            }
        }
    }
}
