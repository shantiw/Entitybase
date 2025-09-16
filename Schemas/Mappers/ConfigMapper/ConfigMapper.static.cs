using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Design.PluralizationServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Schema
{
    public partial class ConfigMapper
    {
        protected static readonly char[] separator = ['_', '-', ' '];

        protected static readonly PluralizationService englishPluralizationService = PluralizationService.CreateEnglishPluralizationService();

        protected static string ToPascalCase(string name)
        {
            string[] words = name.ToLower().Split(separator);

            for (int i = 0; i < words.Length; i++)
            {
                char[] s = words[i].ToCharArray();
                s[0] = char.ToUpper(s[0]);
                words[i] = new string(s);
            }

            return string.Concat(words);
        }

        protected static string ToCamelCase(string name)
        {
            string[] words = name.ToLower().Split(separator);

            for (int i = 1; i < words.Length; i++)
            {
                char[] s = words[i].ToCharArray();
                s[0] = char.ToUpper(s[0]);
                words[i] = new string(s);
            }

            return string.Concat(words);
        }

        protected static string ToSnakeCase(string name)
        {
            string[] words = name.ToLower().Split(separator, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 1) return string.Join("_", words);

            // PascalCase or CamelCase
            string snakeCase = string.Empty;
            for (int i = 0; i < name.Length; i++)
            {
                if (char.IsUpper(name[i]) && i != 0)
                {
                    snakeCase += "_";
                }
                snakeCase += char.ToLower(name[i]);
            }

            return snakeCase.TrimStart('_').TrimEnd('_');
        }

        protected static string ToLower(string name)
        {
            return name.ToLower();
        }

        protected static string ToUpper(string name)
        {
            return name.ToUpper();
        }

        protected static string Singularize(string name)
        {
            int index = name.LastIndexOfAny(separator);
            if (index == -1) return englishPluralizationService.Singularize(name);

            string first = name[..(index + 1)];
            string lastWord = name[(index + 1)..];

            if (englishPluralizationService.IsSingular(lastWord)) return name;

            lastWord = englishPluralizationService.Singularize(lastWord);
            return first + lastWord;
        }

        protected static string Pluralize(string name)
        {
            int index = name.LastIndexOfAny(separator);
            if (index == -1) return englishPluralizationService.Pluralize(name);

            string first = name[..(index + 1)];
            string lastWord = name[(index + 1)..];

            if (englishPluralizationService.IsSingular(lastWord))
            {
                lastWord = englishPluralizationService.Pluralize(lastWord);
                return first + lastWord;
            }

            return name;
        }

    }
}
