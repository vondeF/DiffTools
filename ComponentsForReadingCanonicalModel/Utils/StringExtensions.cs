using System;

namespace RDF_GOST_WORD.Utils
{
    public static class StringExtensions
    {
        public static Guid? ToGuidFromEAID(this string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            var cleaned = id.Replace("EAID_", "").Replace("_", "-");

            if (Guid.TryParse(cleaned, out var guid))
                return guid;

            return null;
        }
    }
}
