using RDF_GOST_WORD.Mapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RDF_GOST_WORD.Models
{
    public class Collaboration : Node
    {
        public List<ClassifierRole> ClassifierRoles;
        public Collaboration(XmlNode element) : base(element)
        {
            ClassifierRoles = new List<ClassifierRole>();
            if (OwnedElements != null)
            {
                foreach (XmlElement xnode in OwnedElements)
                {
                    var localName = xnode.LocalName;
                    if (localName == "ClassifierRole")
                    {
                        var name = xnode.GetAttribute("name");
                        if (Guid.TryParse(name, out var roleId))
                        {
                            var cr = new ClassifierRole(xnode);
                            cr.RoleId = roleId;
                            ClassifierRoles.Add(cr);
                        }
                    }
                }
            }
        }
    }
}
