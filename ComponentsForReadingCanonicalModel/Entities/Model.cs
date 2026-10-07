using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace RDF_GOST_WORD.Models
{
    public class Model: Node
    {
        public Package Package { get; set; }
        public Model(XmlNode element) : base (element)
        {
            XmlElement content = element?["UML:Namespace.ownedElement"];
            foreach (XmlElement xnode in content)
            {
                var localName = xnode.LocalName;
                if (localName == "Package")
                {
                    var package = new Package(xnode);
                    this.Package = package;
                }
            }
        }

        public List<Package> GetAllPackages()
        {
            return GetAllChildPackages(Package);
        }

        public IEnumerable<Association> GetAllAssociations()
        {
            return GetAllPackages().SelectMany(x => x.associations);
        }
        
        public IEnumerable<Class> GetAllClasses()
        {
            return GetAllPackages().SelectMany(x => x.classes);
        }

        public IEnumerable<Generalization> GetAllGeneralizations()
        {
            return GetAllPackages().SelectMany(x => x.generalizations);
        }
        
        public IEnumerable<ClassifierRole> GetAllFootnotes()
        {
            var packages = GetAllPackages();
            return packages.SelectMany(x => x.collaborations.SelectMany(y => y.ClassifierRoles));
        }

        private List<Package> GetAllChildPackages(Package package)
        {
            var list = new List<Package>();
            if (package.classes.Any() || package.generalizations.Any() || package.associations.Any())
                list.Add(package);
            foreach (Package cp in package.childPackages)
            {
                list.AddRange(GetAllChildPackages(cp));
            }
            return list;
        }
    }
}
