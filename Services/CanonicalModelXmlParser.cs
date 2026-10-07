using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System;
using RDF_GOST_WORD.Models;
using RDF_GOST_WORD.Mapper;
using sapphire_diffmaker.Entities;

namespace sapphire_diffmaker.Services
{
    public static class CanonicalModelXmlParser
    {
        public static GostXml Parse(XmlDocument xmlDoc)
        {
            var tempRoot = ProcessDocument(xmlDoc);
            var packageList = tempRoot.Model.GetAllPackages();
            var classes = packageList.SelectMany(x => x.classes);
            var classDictionary = classes.ToDictionary(x => x.Id, x => x);
            var associations = packageList.SelectMany(x => x.associations);
            var attributes = classes.SelectMany(x => x.Attributes);
            tempRoot.FootnoteClassifierRoles = tempRoot.Model.GetAllFootnotes().ToDictionary(x => x.RoleId, x => x);
            var generalizations = packageList.SelectMany(x => x.generalizations);
            
            // Наследования
            foreach (var gen in generalizations)
            {
                var sourceClass = classDictionary[gen.subtype];
                var targetClass = classDictionary[gen.supertype];
                sourceClass.parentClassName = targetClass.Name;
                sourceClass.parentClassStereotype = targetClass.stereotype;
            }
            
            // Классы
            foreach (var item in classes)
            {
                try
                {
                    var objects = tempRoot.GostTaggedValues.Where(x => x.ModelElementId == item.Id).ToList();
                    var gostNumberObject = objects.FirstOrDefault(x => x.GostNumber != null);
                    if (gostNumberObject != null)
                        item.GostNumber = gostNumberObject.GostNumber;
                    var basicProfileExtObject = objects.FirstOrDefault(x => !string.IsNullOrEmpty(x.basicProfileExt));
                    if (basicProfileExtObject != null)
                        item.basicProfileExtName = basicProfileExtObject.basicProfileExt;
                    var footNoteObject = objects.FirstOrDefault(x => !string.IsNullOrEmpty(x.footnotes));
                    if (footNoteObject != null)
                        item.FootNote = new Guid(footNoteObject.footnotes);
                    foreach (var attr in item.Attributes)
                    {
                        try
                        {
                            if (attr.classRef != null)
                            {
                                var foundClass = classDictionary[attr.classifierString];
                                attr.dataTypeUri = new Uri($"{RDFSMapper.GetUriByStereotype(foundClass.stereotype)}{foundClass.Name}");
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }
                catch (Exception e)
                {

                }
            }

            // Ассоциации
            foreach (var assoc in associations)
            {
                try
                {
                    foreach (var end in assoc.AssociationEnds)
                    {
                        var classItem = classDictionary[end.type];
                        end.type = classItem.Name;
                        end.stereotype = classItem.stereotype;
                        if (assoc.stereotype != "rf" && classItem.stereotype == "rf")
                            assoc.stereotype = "rf";
                        if (assoc.stereotype != "so" && classItem.stereotype == "so")
                            assoc.stereotype = "so";
                    }
                }
                catch (Exception e)
                {
                }
            }

            // Атрибуты
            foreach (var attr in attributes)
            {
                var foundAdditionalInfo = classes.Where(x => x.Name == attr.typeClassName).FirstOrDefault(x => !string.IsNullOrEmpty(x.additionalInfo));
                if (foundAdditionalInfo != null)
                {
                    attr.typeOfTypeName = foundAdditionalInfo.additionalInfo;
                }
            }

            return new GostXml(classes?.ToList(), associations?.ToList());
        }

        private static Root ProcessDocument(XmlDocument xmlDoc)
        {
            var xRoot = xmlDoc.DocumentElement;
            var content = xRoot["XMI.content"];
            var root = new Root(content);
            HashSet<string> shit = new HashSet<string>();

            foreach (XmlElement xnode in content)
            {
                if (xnode.LocalName == "Model")
                {
                    root.Model = new Model(xnode);
                }
                else if (xnode.LocalName == "Diagram")
                {
                    root.Nodes.Add(new Diagram(xnode));
                }
                else if (xnode.LocalName == "TaggedValue")
                {
                    if (xnode.Attributes["tag"]?.Value == "GOST_R" && xnode.Attributes["value"] != null)
                    {
                        root.GostTaggedValues.Add(new RootGostTaggedValue()
                        {
                            Id = xnode.Attributes["xmi.id"].Value,
                            ModelElementId = xnode.Attributes["modelElement"].Value,
                            GostNumber = int.Parse(xnode.Attributes["value"].Value.Replace("58651.", ""))
                        });
                    }
                    else if (xnode.Attributes["tag"]?.Value == "basicProfileExt" && xnode.Attributes["value"] != null)
                    {
                        root.GostTaggedValues.Add(new RootGostTaggedValue()
                        {
                            Id = xnode.Attributes["xmi.id"].Value,
                            ModelElementId = xnode.Attributes["modelElement"].Value,
                            basicProfileExt = xnode.Attributes["value"].Value
                        });
                    }
                    else if (xnode.Attributes["tag"]?.Value == "footnotes" && xnode.Attributes["value"] != null)
                    {
                        root.GostTaggedValues.Add(new RootGostTaggedValue()
                        {
                            Id = xnode.Attributes["xmi.id"].Value,
                            ModelElementId = xnode.Attributes["modelElement"].Value,
                            footnotes = xnode.Attributes["value"].Value
                        });
                    }
                }
            }
            return root;
        }

    }
}
