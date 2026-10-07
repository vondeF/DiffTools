using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RDF_GOST_WORD.Models;

namespace sapphire_diffmaker.Entities
{
    public class GostXml
    {
        List<Class> Classes {  get; set; }
        List<Association> Associations { get; set; }
        public GostXml() { }

        public GostXml(List<Class> classes, List<Association> associations) 
        {
            Classes = classes;
            Associations = associations;
        }

        public List<string> GetSoElementsWithouPrefix()
        {
            var res = new List<string>();
            var targetStereotype = "so";

            if (Classes != null)
            {
                foreach (var c in Classes)
                {
                    if (c.stereotype == targetStereotype)
                        res.Add(c.Name);

                    foreach (var a in c.Attributes)
                    {
                        if (a.stereotype == targetStereotype)
                            res.Add($"{c.Name}.{a.Name}");
                    }
                }
            }

            if (Associations != null)
            {
                foreach (var a in Associations)
                {
                    if (a.stereotype == targetStereotype)
                    {
                        res.Add(a.ReverseAssociationName);
                        res.Add(a.DirectAssociationName);
                    }
                }
            }

            return res.Where(x => !string.IsNullOrEmpty(x)).ToList();
        }
    }
}
