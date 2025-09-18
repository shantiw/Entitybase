using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Schema
{
    public partial class SchemaGenerator
    {
        protected static bool IsKey(IEnumerable<string> propertyNames, XElement xEntityType)
        {
            XElement? xKey = xEntityType.Element(SchemaVocab.Key);
            if (xKey == null) return false;

            return IsUnique(propertyNames, xKey);
        }

        protected static XElement? FindUnique(IEnumerable<string> propertyNames, XElement xEntityType)
        {
            foreach (XElement xUnique in xEntityType.Elements(SchemaVocab.Unique))
            {
                if (IsUnique(propertyNames, xUnique)) return xUnique;
            }

            return null;
        }

        private static bool IsUnique(IEnumerable<string> propertyNames, XElement xUnique)
        {
            if (propertyNames.Count() != xUnique.Elements(SchemaVocab.PropertyRef).Count()) return false;

            IEnumerable<string> uniquePropertyNames = xUnique.Elements(SchemaVocab.PropertyRef).Select(p => p.GetAttributeValue(SchemaVocab.Name));

            foreach (string propertyName in propertyNames)
            {
                if (!uniquePropertyNames.Contains(propertyName)) return false;
            }

            return true;
        }

        protected static XElement GetEntityType(string tableName, XElement schema)
        {
            return schema.Elements(SchemaVocab.EntityType).Single(e => e.GetAttributeValue(SchemaVocab.TableName) == tableName);
        }

        protected static XElement GetProperty(DataColumn dataColumn, XElement xEntityType)
        {
            string columnName = dataColumn.ColumnName;

            XElement? xProperty = xEntityType.Elements(SchemaVocab.Property).SingleOrDefault(p => p.GetAttributeValue(SchemaVocab.ColumnName) == columnName);
            if (xProperty == null)
            {
                columnName = ToSpaceCase(columnName);
                xProperty = GetProperty(columnName, xEntityType);
            }

            return xProperty;
        }

        protected static XElement GetProperty(string columnName, XElement xEntityType)
        {
            return xEntityType.Elements(SchemaVocab.Property).Single(p => p.GetAttributeValue(SchemaVocab.ColumnName) == columnName);
        }

        protected static DataTable GetDataTable(string tableName, DataSet dataSet)
        {
            DataTable? dataTable = dataSet.Tables[tableName];

            if (dataTable == null)
            {
                string name = ToSnakeCase(tableName);
                dataTable = dataSet.Tables[name];
            }

            if (dataTable == null) throw new Exception();

            return dataTable;
        }

        protected static DataColumn GetDataColumn(string columnName, DataTable dataTable)
        {
            DataColumn? dataColumn = dataTable.Columns[columnName];

            if (dataColumn == null)
            {
                string name = ToSnakeCase(columnName);
                dataColumn = dataTable.Columns[name];
            }

            if (dataColumn == null) throw new Exception();

            return dataColumn;
        }

        protected static string ToSnakeCase(string spaceCaseString)
        {
            return string.Join("_", spaceCaseString.Split(' '));
        }

        protected static string ToSpaceCase(string snakeCaseString)
        {
            return string.Join(" ", snakeCaseString.Split('_'));
        }

    }
}
