using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Schema
{
    public class PascalCaseMapper(XElement databaseSchema, DataSet schemaDataSet) : Mapper(databaseSchema, schemaDataSet)
    {
        public override (string entityTypeName, string entitySetName) GetEntityTypeName(string tableName)
        {
            string[] words = tableName.Split('_', '-', ' ');

            string lastWord = words[^1];
            if (IsSingular(lastWord))
            {
                string entityTypeName = ToPascalCase(words);
                words[^1] = Pluralize(lastWord);
                string entitySetName = ToPascalCase(words);
                return (entityTypeName, entitySetName);
            }
            else
            {
                string entitySetName = ToPascalCase(words);
                words[^1] = Singularize(lastWord);
                return (ToPascalCase(words), entitySetName);
            }
        }

        public override string GetPropertyName(string columnName, string tableName)
        {
            string[] words = columnName.Split('_', '-', ' ');
            return ToPascalCase(words);
        }

        public override string? GetSequenceName(string columnName, string tableName)
        {
            return null;
        }

    }
}
