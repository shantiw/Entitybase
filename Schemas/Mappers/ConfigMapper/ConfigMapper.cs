using Microsoft.VisualBasic;
using System.Data;
using System.Xml.Linq;

namespace Shantiw.Data.Schema
{
    public partial class ConfigMapper : Mapper, IMapper
    {
        protected readonly string EntityTypeNameFormat;
        protected readonly string[]? EntityTypeName_Functions = null;

        protected readonly string EntitySetNameFormat;
        protected readonly string[]? EntitySetName_Functions = null;

        protected readonly string PropertyNameFormat;
        protected readonly string[]? PropertyName_Column_Functions = null;
        protected readonly string[]? PropertyName_TableName_Functions = null;

        protected readonly string? SequenceNameFormat = null;

        public ConfigMapper(XElement mapping) : base(mapping)
        {
            XElement xMapper = mapping.Elements(MapperVocab.Mapper).Single();

            //
            XElement xEntityTypeMapping = xMapper.Elements(MapperVocab.EntityTypeMapping).Single();
            EntityTypeNameFormat = xEntityTypeMapping.GetAttributeValue(ConfigMapperVocab.Format);
            XElement? xFunctions = xEntityTypeMapping.Element(ConfigMapperVocab.Functions);
            if (xFunctions != null)
            {
                EntityTypeName_Functions = GetFunctions(xFunctions.Elements(ConfigMapperVocab.Function));
            }

            //
            XElement xEntitySetMapping = xMapper.Elements(MapperVocab.EntitySetMapping).Single();
            EntitySetNameFormat = xEntitySetMapping.GetAttributeValue(ConfigMapperVocab.Format);
            xFunctions = xEntitySetMapping.Element(ConfigMapperVocab.Functions);
            if (xFunctions != null)
            {
                EntitySetName_Functions = GetFunctions(xFunctions.Elements(ConfigMapperVocab.Function));
            }

            //
            XElement xPropertyMapping = xMapper.Elements(MapperVocab.PropertyMapping).Single();
            PropertyNameFormat = xPropertyMapping.GetAttributeValue(ConfigMapperVocab.Format);
            xFunctions = xPropertyMapping.Element(ConfigMapperVocab.Functions);
            if (xFunctions != null)
            {
                PropertyName_Column_Functions = GetFunctions(xFunctions.Elements(ConfigMapperVocab.Function)
                    .Where(f => f.GetAttributeValue(ConfigMapperVocab.Parameter) == ConfigMapperVocab.ColumnName));

                PropertyName_TableName_Functions = GetFunctions(xFunctions.Elements(ConfigMapperVocab.Function)
                    .Where(f => f.GetAttributeValue(ConfigMapperVocab.Parameter) == ConfigMapperVocab.TableName));
            }

            //
            XElement? xSequenceAttaching = xMapper.Elements(MapperVocab.SequenceAttaching).SingleOrDefault();
            if (xSequenceAttaching != null)
                SequenceNameFormat = xSequenceAttaching.GetAttributeValue(ConfigMapperVocab.Format);
        }

        private static string[]? GetFunctions(IEnumerable<XElement> xFunctions) // constrctor
        {
            int count = xFunctions.Count();
            if (count == 0) return null;

            string[] functions;
            if (count == 1)
            {
                functions = new string[1];
                functions[0] = xFunctions.Single().GetAttributeValue(ConfigMapperVocab.Name);
            }
            else
            {
                functions = [.. xFunctions.OrderBy(f => f.GetAttributeValue(ConfigMapperVocab.Order))
                            .Select(f => f.GetAttributeValue(ConfigMapperVocab.Name))];
            }

            return functions;
        }

        public override (string entityTypeName, string entitySetName) GetEntityTypeName(string tableName)
        {
            (string? typeName, string? setName) = GetNullableEntityTypeName(tableName);
            if (typeName != null && setName != null) return (typeName, setName);

            //
            string entityTypeName = GetEntity_Name(EntityTypeName_Functions, tableName, EntityTypeNameFormat,
                ConfigMapperVocab.FuncsTableNamePlaceholder, ConfigMapperVocab.TableNamePlaceholder);

            string entitySetName = GetEntity_Name(EntitySetName_Functions, tableName, EntitySetNameFormat,
                ConfigMapperVocab.FuncsTableNamePlaceholder, ConfigMapperVocab.TableNamePlaceholder);

            AddEntityTypeName(entityTypeName, entitySetName, tableName);

            return (entityTypeName, entitySetName);
        }

        private static string GetEntity_Name(string[]? functions, string tableName, string nameFormat, string funcsNamePlaceholder, string namePlaceholder) // GetEntityTypeName
        {
            string? funcsName = GetFuncsName(functions, tableName);

            if (funcsName == null)
            {
                string format = nameFormat.Replace(namePlaceholder, "{0}");
                return string.Format(format, tableName);
            }
            else
            {
                string format = nameFormat.Replace(funcsNamePlaceholder, "{0}");
                if (nameFormat.Contains(namePlaceholder))
                {
                    format = format.Replace(namePlaceholder, "{1}");
                    return string.Format(format, funcsName, tableName);
                }
                else
                {
                    return string.Format(format, funcsName);
                }
            }
        }

        public override string GetPropertyName(string columnName, string tableName)
        {
            string? propertyName = GetNullablePropertyName(columnName, tableName);
            if (propertyName != null) return propertyName;

            //
            int index = 0;
            Dictionary<int, string> index_parameters = new Dictionary<int, string>();
            string format = PropertyNameFormat;
            string? columnNameFuncsName = GetFuncsName(PropertyName_Column_Functions, columnName);
            if (columnNameFuncsName != null)
            {
                format = format.Replace(ConfigMapperVocab.FuncsColumnNamePlaceholder, "{" + index + "}");
                index_parameters.Add(index, columnNameFuncsName);
                index++;
            }

            string? tableNameFuncsName = GetFuncsName(PropertyName_TableName_Functions, tableName);
            if (tableNameFuncsName != null)
            {
                format = format.Replace(ConfigMapperVocab.FuncsTableNamePlaceholder, "{" + index + "}");
                index_parameters.Add(index, tableNameFuncsName);
                index++;
            }

            if (format.Contains(ConfigMapperVocab.ColumnNamePlaceholder))
            {
                format = format.Replace(ConfigMapperVocab.ColumnNamePlaceholder, "{" + index + "}");
                index_parameters.Add(index, columnName);
                index++;
            }

            if (format.Contains(ConfigMapperVocab.TableNamePlaceholder))
            {
                format = format.Replace(ConfigMapperVocab.TableNamePlaceholder, "{" + index + "}");
                index_parameters.Add(index, tableName);
            }

            string name = string.Format(format, [.. index_parameters.Values]);

            AddPropertyName(name, columnName, tableName);

            return name;
        }

        private static string? GetFuncsName(string[]? functions, string value)
        {
            if (functions == null || functions.Length == 0) return null;

            string result = value;
            if (functions != null)
            {
                foreach (string functionName in functions)
                {
                    result = Invoke(functionName, result);
                }
            }
            return result;
        }

        public override string? GetSequenceName(string columnName, string tableName)
        {
            string? sequenceName = base.GetSequenceName(columnName, tableName);
            if (sequenceName != null) return sequenceName;

            if (SequenceNameFormat == null || SequenceNameFormat == ConfigMapperVocab.NullPlaceholder) return null;

            string format = SequenceNameFormat.Replace(ConfigMapperVocab.TableNamePlaceholder, "{0}").Replace(ConfigMapperVocab.ColumnNamePlaceholder, "{1}");
            string name = string.Format(format, tableName, columnName);

            AddSequenceName(name, columnName, tableName);

            return name;
        }

        protected static string Invoke(string functionName, string value)
        {
            return TryInvoke(functionName, value, out string result) ? result : throw new NotSupportedException(functionName + "is not supported.");
        }

        protected static bool TryInvoke(string functionName, string value, out string result)
        {
            result = functionName switch
            {
                "ToLower" => ToLower(value),
                "ToUpper" => ToUpper(value),
                "ToPascalCase" => ToPascalCase(value),
                "ToCamelCase" => ToCamelCase(value),
                "ToSnakeCase" => ToSnakeCase(value),
                "Singularize" => Singularize(value),
                "Pluralize" => Pluralize(value),
                _ => value,
            };

            return result != null;
        }

    }
}
