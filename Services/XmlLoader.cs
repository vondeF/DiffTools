using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;

namespace sapphire_diffmaker.Services
{
    public static class XmlLoader
    {
        /// <summary>
        /// Загрузить XMI файл по указанному пути
        /// </summary>
        public static XmlDocument LoadXmiFromPath(string path)
        {
            if (!File.Exists(path))
                return null;

            var doc = new XmlDocument();
            doc.Load(path);
            return doc;
        }
    }
}
