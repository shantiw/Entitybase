using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Shantiw.Data.Schema
{
    public partial class SchemaGenerator(XElement databaseSchema, DataSet schemaDataSet, XElement mapping)
    {
        protected readonly XElement DatabaseSchema = databaseSchema;
        protected readonly DataSet SchemaDataSet = schemaDataSet;
        protected readonly IMapper Mapper = MapperFactory.Create(mapping);

        public SchemaGenerator(string storeSchemaFileName, string mappingFileName)
            : this(XElement.Load(storeSchemaFileName), XElement.Load(mappingFileName))
        {
        }

        public SchemaGenerator(XElement storeSchema, XElement mapping)
            : this(storeSchema.Elements().Single(e => e.Name == DbSchemaVocab.Schema && e.Attribute(DbSchemaVocab.Database) != null),
                  CreateSchemaDataSet(storeSchema), mapping)
        {
        }

        private static DataSet CreateSchemaDataSet(XElement storeSchema)
        {
            XElement dataSetSchema = storeSchema.Elements().Single(e => e.GetNamespaceOfPrefix("xs") != null && e.Attribute("id") != null);
            using XmlReader xmlReader = dataSetSchema.CreateReader();
            DataSet SchemaDataSet = new();
            SchemaDataSet.ReadXmlSchema(xmlReader);
            return SchemaDataSet;
        }

        public void WriteXmlSchema(string fileName)
        {
            XElement schema = Generate();
            schema.Save(fileName);
        }

        public virtual XElement Generate()
        {
            XElement schema = new(SchemaVocab.Schema,
                 new XAttribute(SchemaVocab.Model, DatabaseSchema.GetAttributeValue(DbSchemaVocab.Database)),
                 new XAttribute(SchemaVocab.CreatedAt, $"{Environment.MachineName}, {DateTime.Now}"));

            AddEntityTypes(schema);

            SetRelationships(schema);

            SetAnnotations(schema);

            return schema;
        }

        protected void AddEntityTypes(XElement schema)
        {
            foreach (XElement xTable in DatabaseSchema.Elements(DbSchemaVocab.Table))
            {
                string tableName = xTable.GetAttributeValue(DbSchemaVocab.Name);
                DataTable dataTable = GetDataTable(tableName, SchemaDataSet);
                XElement xEntityType = CreateEntityType(xTable, tableName, dataTable);

                schema.Add(xEntityType);

                //
                object? caption = dataTable.ExtendedProperties[nameof(DataColumn.Caption)]; // DataColumn.Caption has borrowed because DataTable does not have the property.
                if (caption != null)
                {
                    XComment xComment = new(" " + (string)caption + " ");
                    xEntityType.AddBeforeSelf(xComment);
                }
            }
        }

        protected XElement CreateEntityType(XElement xTable, string tableName, DataTable dataTable)
        {
            var (entityTypeName, entitySetName) = Mapper.GetEntityTypeName(tableName);
            XElement xEntityType = new(SchemaVocab.EntityType,
                new XAttribute(SchemaVocab.Name, entityTypeName),
                new XAttribute(SchemaVocab.EntitySetName, entitySetName),
                new XAttribute(SchemaVocab.TableName, tableName));

            string? viewUpdatable = xTable.GetNullableAttributeValue(DbSchemaVocab.ViewUpdatable);
            if (viewUpdatable != null && !bool.Parse(viewUpdatable))
                xEntityType.SetAttributeValue(SchemaVocab.ReadOnly, true);

            //
            foreach (XElement xColumn in xTable.Elements(DbSchemaVocab.Column))
            {
                string columnName = xColumn.GetAttributeValue(DbSchemaVocab.Name);
                DataColumn dataColumn = GetDataColumn(columnName, dataTable);
                XElement xProperty = CreateProperty(xColumn, tableName, dataTable);

                xEntityType.Add(xProperty);

                //
                if (dataColumn.Caption != columnName)
                {
                    XComment xComment = new(" " + dataColumn.Caption + " ");
                    xProperty.AddBeforeSelf(xComment);
                }
            }

            //
            XElement? xKey = CreateKey(xTable, dataTable, xEntityType);
            if (xKey != null) xEntityType.AddFirst(xKey);

            //
            xEntityType.Add(CreateUniques(xTable, xEntityType));

            return xEntityType;
        }

        protected XElement CreateProperty(XElement xColumn, string tableName, DataTable dataTable)
        {
            string columnName = xColumn.GetAttributeValue(DbSchemaVocab.Name);

            DataColumn dataColumn = GetDataColumn(columnName, dataTable);

            string propertyName = Mapper.GetPropertyName(columnName, tableName);
            XElement xProperty = new(SchemaVocab.Property,
                new XAttribute(SchemaVocab.Name, propertyName),
                new XAttribute(SchemaVocab.Type, dataColumn.DataType.Name),
                new XAttribute(SchemaVocab.Nullable, xColumn.GetAttributeValue(DbSchemaVocab.Nullable)),
                new XAttribute(SchemaVocab.ColumnName, columnName));

            //
            if ((dataColumn.DataType == typeof(string) || dataColumn.DataType.IsArray) && dataColumn.MaxLength != -1)
                xProperty.SetAttributeValue(SchemaVocab.MaxLength, dataColumn.MaxLength);

            // Identity or Sequence+Trigger, DatabaseGenerated
            string? autoIncrement = xColumn.GetNullableAttributeValue(DbSchemaVocab.AutoIncrement);
            if (autoIncrement != null && bool.Parse(autoIncrement))
            {
                xProperty.SetAttributeValue(SchemaVocab.AutoGenerated, true);
            }
            else
            {
                // "SELECT NEXT VALUE FOR {SequenceName}", ServiceGenerated
                string? sequenceName = Mapper.GetSequenceName(columnName, tableName);
                if (sequenceName != null)
                {
                    if (DatabaseSchema.Elements(DbSchemaVocab.Sequence).Any(s => s.GetAttributeValue(DbSchemaVocab.Name) == sequenceName))
                    {
                        xProperty.SetAttributeValue(SchemaVocab.AutoGenerated, true);
                        xProperty.SetAttributeValue(SchemaVocab.SequenceName, sequenceName);
                    }
                }
            }

            //
            if (dataColumn.DefaultValue == DBNull.Value)
            {
                string? defaultValue = xColumn.GetNullableAttributeValue(DbSchemaVocab.DefaultValue);
                if (defaultValue != null)
                    xProperty.SetAttributeValue(SchemaVocab.DefaultValue, defaultValue);
            }
            else
            {
                xProperty.SetAttributeValue(SchemaVocab.DefaultValue, dataColumn.DefaultValue);
            }

            //
            string? concurrencyMode = xColumn.GetNullableAttributeValue(DbSchemaVocab.ConcurrencyMode);
            if (concurrencyMode != null && concurrencyMode == DbSchemaVocab.ConcurrencyModeFixed)
                xProperty.SetAttributeValue(SchemaVocab.ConcurrencyCheck, true);

            return xProperty;
        }

        protected static XElement? CreateKey(XElement xTable, DataTable dataTable, XElement xEntityType)
        {
            XElement xKey;

            XElement? xPrimaryKey = xTable.Element(DbSchemaVocab.Key);
            if (xPrimaryKey == null)
            {
                xKey = new(SchemaVocab.Key);
                foreach (DataColumn dataColumn in dataTable.PrimaryKey)
                {
                    XElement xProperty = GetProperty(dataColumn, xEntityType);
                    xKey.Add(new XElement(SchemaVocab.PropertyRef,
                        new XAttribute(SchemaVocab.Name, xProperty.GetAttributeValue(SchemaVocab.Name))));
                }
            }
            else
            {
                xKey = CreateUnique(xPrimaryKey, xEntityType);
                xKey.Name = SchemaVocab.Key;
            }

            return (xKey.HasElements) ? xKey : null;
        }

        protected static IEnumerable<XElement> CreateUniques(XElement xTable, XElement xEntityType)
        {
            List<XElement> xUniques = [];

            foreach (XElement xUniqueConstraint in xTable.Elements(DbSchemaVocab.Unique))
            {
                XElement xUnique = CreateUnique(xUniqueConstraint, xEntityType);
                xUniques.Add(xUnique);
            }

            return xUniques;
        }

        protected static XElement CreateUnique(XElement xUniqueConstraint, XElement xEntityType)
        {
            XElement xUnique = new(SchemaVocab.Unique);

            foreach (XElement xColumnRef in xUniqueConstraint.Elements(DbSchemaVocab.ColumnRef))
            {
                string columnName = xColumnRef.GetAttributeValue(DbSchemaVocab.Name);
                XElement xProperty = GetProperty(columnName, xEntityType);
                xUnique.Add(new XElement(SchemaVocab.PropertyRef,
                    new XAttribute(SchemaVocab.Name, xProperty.GetAttributeValue(SchemaVocab.Name))));
            }

            return xUnique;
        }

    }
}
