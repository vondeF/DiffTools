using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sapphire_diffmaker.Services
{
    public static class Translator
    {
        private static readonly Dictionary<char, string> Map = new Dictionary<char, string>
        {
            {'а', "a"}, {'б', "b"}, {'в', "v"}, {'г', "g"}, {'д', "d"},
            {'е', "e"}, {'ё', "e"}, {'ж', "zh"}, {'з', "z"}, {'и', "i"},
            {'й', "y"}, {'к', "k"}, {'л', "l"}, {'м', "m"}, {'н', "n"},
            {'о', "o"}, {'п', "p"}, {'р', "r"}, {'с', "s"}, {'т', "t"},
            {'у', "u"}, {'ф', "f"}, {'х', "h"}, {'ц', "ts"}, {'ч', "ch"},
            {'ш', "sh"}, {'щ', "sch"}, {'ъ', ""}, {'ы', "y"}, {'ь', ""},
            {'э', "e"}, {'ю', "yu"}, {'я', "ya"},
        };

        public static string ToLatin(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var sb = new System.Text.StringBuilder(input.Length);
            foreach (char c in input)
            {
                char lower = char.ToLower(c);
                if (Map.TryGetValue(lower, out var latin))
                {
                    // сохраняем регистр
                    if (char.IsUpper(c) && latin.Length > 0)
                        sb.Append(char.ToUpper(latin[0]) + latin.Substring(1));
                    else
                        sb.Append(latin);
                }
                else
                {
                    sb.Append(c); // цифры, знаки, латиница
                }
            }
            return sb.ToString();
        }
    }
}
