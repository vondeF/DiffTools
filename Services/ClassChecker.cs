using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monitel.Mal.Context.CIM16;

namespace sapphire_diffmaker.Services
{
    public static class ClassChecker
    {
        private static readonly HashSet<string> _cimClassNames;

        static ClassChecker()
        {
            // Получаем все типы из пространства имен CIM16
            var assembly = typeof(IdentifiedObject).Assembly; // Берем любой класс из CIM16
            var cimTypes = assembly.GetTypes()
                .Where(t => t.Namespace == "Monitel.Mal.Context.CIM16" && t.IsClass)
                .ToList();

            _cimClassNames = new HashSet<string>(cimTypes.Select(t => t.Name));
        }

        public static bool IsRightClass(string className)
        {
            return !string.IsNullOrEmpty(className) && _cimClassNames.Contains(className);
        }
    }
}
