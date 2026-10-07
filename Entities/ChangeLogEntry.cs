using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sapphire_diffmaker.Entities
{
    public class ChangeLogEntry
    {
        public string Date { get; set; }
        public string Version { get; set; }
        public string Changes { get; set; }
    }
}
