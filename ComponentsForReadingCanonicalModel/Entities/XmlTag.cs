using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDF_GOST_WORD.Models
{
    public class TaggedValue
    {
        public string name { get; set; }
        public string value { get; set; }

        public TaggedValue(string name, string value)
        {
            this.name = name;
            this.value = value;
        }
    }
}
