using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Schema
{
    public class SimpleMapper(XElement databaseSchema, DataSet schemaDataSet) : Mapper(databaseSchema, schemaDataSet)
    {
        public override (string entityTypeName, string entitySetName) GetEntityTypeName(string tableName)
        {
            string entitySetName = tableName.EndsWith('s') ? tableName + "es" : tableName + "s";
            return (tableName, entitySetName);
        }

        public override string GetPropertyName(string columnName, string tableName)
        {
            return columnName;
        }

        public override string? GetSequenceName(string columnName, string tableName)
        {
            return null;
        }

    }
}
