using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Schema
{
    public partial class Mapper
    {
        private readonly Dictionary<string, string> Table_EntityTypes = [];
        private readonly Dictionary<string, string> Table_EntitySets = [];
        private readonly Dictionary<string, Dictionary<string, string>> EntityType_Column_Propertys = [];

        private readonly Dictionary<string, string> TableColumn_Sequences = [];

        private void Load()
        {
            string? fileName = Mapping.GetNullableAttributeValue(MappingVocab.File);
            if (fileName == null)
            {
                ReadXml(Mapping);
                return;
            }

            string extension = Path.GetExtension(fileName).ToLower();
            switch (extension)
            {
                case ".xml":
                    ReadXml(XElement.Load(fileName));
                    break;
                case ".json":
                    ReadJson(fileName);
                    break;
                case ".csv":
                    ReadCsv(fileName);
                    break;
                default:
                    throw new NotSupportedException(extension);
            }
        }

        /// <summary>
        /// {TableName},{EntityTypeName},{EntitySetName}
        /// {ColumnName},{PropertyName},[{SequenceName}]
        /// </summary>
        private void ReadCsv(string fileName)
        {
            int index = 1;
            string? tableName = null;
            string? line;
            using StreamReader reader = new(fileName);
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Trim() == string.Empty) continue;

                string[] values = line.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (values.Length == 3)
                {
                    if (values[2].StartsWith('[') && values[2].EndsWith(']'))
                    {
                        string columnName = values[0];
                        string propertyName = values[1];
                        string sequenceName = values[2];

                        if (string.IsNullOrWhiteSpace(tableName)) throw new FormatException($"Data is invalid at line {index}.");

                        AddPropertyName(propertyName, columnName, tableName);
                        AddSequenceName(sequenceName, columnName, tableName);
                    }
                    else
                    {
                        tableName = values[0];
                        string entityTypeName = values[1];
                        string entitySetName = values[2];
                        AddEntityTypeName(entityTypeName, entitySetName, tableName);
                    }
                }
                else if (values.Length == 2)
                {
                    string columnName = values[0];
                    string propertyName = values[1];

                    if (string.IsNullOrWhiteSpace(tableName)) throw new FormatException($"Data is invalid at line {index}.");

                    AddPropertyName(propertyName, columnName, tableName);
                }

                index++;
            }
        }

        private void ReadJson(string fileName)
        {
            using FileStream stream = File.OpenRead(fileName);
            using JsonDocument document = JsonDocument.Parse(stream);

            JsonElement root = document.RootElement;
            foreach (JsonElement jEntityTypeMapping in root.GetProperty(MappingVocab.EntityTypeMapping).EnumerateArray())
            {
                string entityTypeName = jEntityTypeMapping.GetProperty(MappingVocab.Name).ToString();
                string entitySetName = jEntityTypeMapping.GetProperty(MappingVocab.EntitySetName).ToString();
                string tableName = jEntityTypeMapping.GetProperty(MappingVocab.TableName).ToString();
                AddEntityTypeName(entityTypeName, entitySetName, tableName);

                foreach (JsonElement jPropertyMapping in jEntityTypeMapping.GetProperty(MappingVocab.PropertyMapping).EnumerateArray())
                {
                    string propertyName = jPropertyMapping.GetProperty(MappingVocab.Name).ToString();
                    string columnName = jPropertyMapping.GetProperty(MappingVocab.ColumnName).ToString();
                    AddPropertyName(propertyName, columnName, tableName);
                }
            }

            //
            foreach (JsonElement jSequenceAttaching in root.GetProperty(MappingVocab.SequenceAttaching).EnumerateArray())
            {
                string sequenceName = jSequenceAttaching.GetProperty(MappingVocab.Name).ToString();
                string columnName = jSequenceAttaching.GetProperty(MappingVocab.ColumnName).ToString();
                string tableName = jSequenceAttaching.GetProperty(MappingVocab.TableName).ToString();
                AddSequenceName(sequenceName, columnName, tableName);
            }
        }

        private void ReadXml(XElement xMapping)
        {
            foreach (XElement xEntityTypeMapping in xMapping.Elements(MappingVocab.EntityTypeMapping))
            {
                string tableName = xEntityTypeMapping.GetAttributeValue(MappingVocab.TableName);
                string entityTypeName = xEntityTypeMapping.GetAttributeValue(MappingVocab.Name);
                string entitySetName = xEntityTypeMapping.GetAttributeValue(MappingVocab.EntitySetName);
                AddEntityTypeName(entityTypeName, entitySetName, tableName);

                foreach (XElement xPropertyMapping in xEntityTypeMapping.Elements(MappingVocab.PropertyMapping))
                {
                    string columnName = xPropertyMapping.GetAttributeValue(MappingVocab.ColumnName);
                    string propertyName = xPropertyMapping.GetAttributeValue(MappingVocab.Name);
                    AddPropertyName(propertyName, columnName, tableName);
                }
            }

            //
            foreach (XElement xSequenceAttaching in xMapping.Elements(MappingVocab.SequenceAttaching))
            {
                string tableName = xSequenceAttaching.GetAttributeValue(MappingVocab.TableName);
                string columnName = xSequenceAttaching.GetAttributeValue(MappingVocab.ColumnName);
                string sequenceName = xSequenceAttaching.GetAttributeValue(MappingVocab.Name);
                AddSequenceName(sequenceName, columnName, tableName);
            }
        }

        protected (string? entityTypeName, string? entitySetName) GetNullableEntityTypeName(string tableName)
        {
            return Table_EntityTypes.TryGetValue(tableName, out string? value) ?
                 (value, Table_EntitySets[tableName]) : (null, null);
        }

        protected string? GetNullablePropertyName(string columnName, string tableName)
        {
            if (Table_EntityTypes.TryGetValue(tableName, out string? entityTypeName))
            {
                if (EntityType_Column_Propertys.TryGetValue(entityTypeName, out Dictionary<string, string>? column_Propertys))
                {
                    if (column_Propertys.TryGetValue(columnName, out string? value))
                    {
                        return value;
                    }
                }   
            }

            return null;
        }

        protected void AddEntityTypeName(string entityTypeName, string entitySetName, string tableName)
        {
            if (Table_EntityTypes.ContainsKey(tableName))
                throw new ArgumentException($"An item with the same key has already been added. TableName: {tableName}");

            if (Table_EntityTypes.ContainsValue(entityTypeName))
                throw new ArgumentException($"An item with the same key has already been added. EntityTypeName: {entityTypeName}");

            if (Table_EntitySets.ContainsValue(entitySetName))
                throw new ArgumentException($"An item with the same key has already been added. EntityTypeName: {entitySetName}");

            Table_EntityTypes.Add(tableName, entityTypeName);
            Table_EntitySets.Add(tableName, entitySetName);
        }

        protected void AddPropertyName(string propertyName, string columnName, string tableName)
        {
            string entityTypeName = Table_EntityTypes[tableName];

            Dictionary<string, string> column_Propertys;
            if (EntityType_Column_Propertys.TryGetValue(entityTypeName, out Dictionary<string, string>? value))
            {
                column_Propertys = value;

                if (column_Propertys.ContainsKey(columnName))
                    throw new ArgumentException($"An item with the same key has already been added. ColumnName: {columnName}, TableName: {tableName}");

                if (column_Propertys.ContainsValue(propertyName))
                    throw new ArgumentException($"An item with the same key has already been added. PropertyName: {propertyName}");
            }
            else
            {
                column_Propertys = [];
                EntityType_Column_Propertys.Add(entityTypeName, column_Propertys);
            }

            column_Propertys.Add(columnName, propertyName);
        }

        protected void AddSequenceName(string sequenceName, string columnName, string tableName)
        {
            string key = GetTableColumnKey(columnName, tableName);
            if (TableColumn_Sequences.ContainsKey(key))
                throw new ArgumentException($"An item with the same key has already been added. ColumnName: {columnName}, TableName: {tableName}");

            TableColumn_Sequences.Add(key, sequenceName);
        }

        private static string GetTableColumnKey(string columnName, string tableName)
        {
            return $"{tableName}-{columnName}";
        }

        protected bool EntityTypeNameExists(string entityTypeName)
        {
            return Table_EntityTypes.ContainsValue(entityTypeName);
        }

        protected bool EntitySetNameExists(string entitySetName)
        {
            return Table_EntitySets.ContainsValue(entitySetName);
        }

        protected bool PropertyNameExists(string propertyName, string entityTypeName)
        {
            if (EntityType_Column_Propertys.TryGetValue(entityTypeName, out Dictionary<string, string>? column_Propertys))
            {
                return column_Propertys.ContainsValue(propertyName);
            }

            return false;
        }

    }
}
