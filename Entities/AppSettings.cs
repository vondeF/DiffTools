using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sapphire_diffmaker.Entities
{
    public class AppSettings
    {
        public string AssemblyResolvePath { get; set; }
        public string ProvidersDB {  get; set; }
        public string ModelNavigatorPath { get; set; }
        public string GostXmlPath { get; set; }
        public string ConnectionStringMappingService { get; set; }
        public string ServerPath { get; set; }
        public GostDatabases GostDatabases { get; set; }
        public List<DatabaseInfo> Databases { get; set; }
    }

    public class DatabaseInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class GostDatabases
    {
        public string GostDbName { get; set; }
        public string NonGostDbName { get; set; }
    }
}
