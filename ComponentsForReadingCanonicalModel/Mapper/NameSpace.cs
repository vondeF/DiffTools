using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDF_GOST_WORD.Mapper
{
    public class NameSpace
    {
        public string key { get; set; }
        public Uri value { get; set; }

        public NameSpace(string name, string uri)
        {
            key = name;
            value = new Uri(uri);
        }
    }
}
