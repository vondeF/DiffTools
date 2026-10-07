using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDF_GOST_WORD.Services
{
    public class UnitsDetectorService
    {
        private static UnitsDetectorService _instance;
        public static UnitsDetectorService Instance => _instance = new UnitsDetectorService();
        public Dictionary<string, string> Units { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Multipliers { get; set; } = new Dictionary<string, string>();

    }
}
