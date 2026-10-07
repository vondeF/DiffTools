using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace RDF_GOST_WORD.Models
{
    public class ClassifierRole : Node
    {
        public Guid RoleId { get; set; }
        public string Text { get; set; }
        public int Number { get; set; }
        public int Column { get; set; }
        public ClassifierRole(XmlNode element) : base(element)
        {
            var runstate = this.TaggedValues["runstate"];

            string pattern = @"@VAR;Variable=(?<name>[^;]+);Value=(?<value>[^;]*);Op==;@ENDVAR;";

            var matches = Regex.Matches(runstate, pattern);

            var variables = new Dictionary<string, string>();

            foreach (Match match in matches)
            {
                string name = match.Groups["name"].Value;
                string value = match.Groups["value"].Value;
                variables[name] = value;
            }
            Text = variables["text"];
            Number = int.Parse(variables["number"]);
            Column = int.Parse(variables["column"]);
        }
    }
}
