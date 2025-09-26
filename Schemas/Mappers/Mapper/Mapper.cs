using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Schema
{
    public partial class Mapper : IMapper
    {
        protected readonly XElement Mapping;

        public virtual CommnentPolicy EntityTypeCommnentPolicy { get; protected set; } = CommnentPolicy.None;

        public virtual CommnentPolicy PropertyCommnentPolicy { get; protected set; } = CommnentPolicy.None;

        public Mapper(XElement mapping)
        {
            Mapping = mapping;

            //
            XElement? xMapper = Mapping.Element(MapperVocab.Mapper);
            if (xMapper != null)
            {
                XElement? xEntityTypeMapping = xMapper.Elements(MapperVocab.EntityTypeMapping).SingleOrDefault();
                if (xEntityTypeMapping != null)
                {
                    string? displayName = xEntityTypeMapping.GetNullableAttributeValue(MapperVocab.DisplayName);
                    string? description = xEntityTypeMapping.GetNullableAttributeValue(MapperVocab.Description);
                    if (displayName != null && displayName == "{Comment}") EntityTypeCommnentPolicy |= CommnentPolicy.DisplayName;
                    if (description != null && description == "{Comment}") EntityTypeCommnentPolicy |= CommnentPolicy.Description;
                }

                XElement? xPropertyMapping = xMapper.Elements(MapperVocab.PropertyMapping).SingleOrDefault();
                if (xPropertyMapping != null)
                {
                    string? displayName = xPropertyMapping.GetNullableAttributeValue(MapperVocab.DisplayName);
                    string? description = xPropertyMapping.GetNullableAttributeValue(MapperVocab.Description);
                    if (displayName != null && displayName == "{Comment}") PropertyCommnentPolicy |= CommnentPolicy.DisplayName;
                    if (description != null && description == "{Comment}") PropertyCommnentPolicy |= CommnentPolicy.Description;
                }
            }

            //
            Load();
        }

        public virtual (string entityTypeName, string entitySetName) GetEntityTypeName(string tableName)
        {
            return (Table_EntityTypes[tableName], Table_EntitySets[tableName]);
        }

        public virtual string GetPropertyName(string columnName, string tableName)
        {
            string entityTypeName = Table_EntityTypes[tableName];
            return EntityType_Column_Propertys[entityTypeName][columnName];
        }

        public virtual string? GetSequenceName(string columnName, string tableName)
        {
            string key = GetTableColumnKey(columnName, tableName);
            TableColumn_Sequences.TryGetValue(key, out string? sequenceName);
            return sequenceName;
        }

    }
}
