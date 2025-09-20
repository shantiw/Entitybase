using System.Data;
using System.Data.Common;
using System.Xml.Linq;
using static System.Formats.Asn1.AsnWriter;

namespace Shantiw.Data.Schema
{
    public abstract partial class DbSchemaExtractor
    {
        protected readonly string ConnectionString;
        protected readonly DbConnection Connection;

        protected abstract DbConnection CreateConnection();
        protected abstract DbDataAdapter CreateDataAdapter();
        protected abstract Database GetDatabase();

        protected DbSchemaExtractor(string connectionString)
        {
            ConnectionString = connectionString;
            Connection = CreateConnection();
        }

        public void WriteXmlSchema(string fileName)
        {
            XElement dbSchema = Extract();

            dbSchema.Save(fileName);
        }

        public XElement Extract()
        {
            XElement databaseSchema = Extract(out DataSet schemaDataSet);

            XElement dataSetSchema = GetXml(schemaDataSet);

            XElement root = new(STORE_SCHEMA);

            root.Add(new XComment(DATABASE_SCHEMA_COMMENT),
                databaseSchema,
                new XComment(DATASET_SCHEMA_COMMENT),
                dataSetSchema);

            return root;
        }

        public XElement Extract(out DataSet schemaDataSet)
        {
            Database database = GetDatabase();

            schemaDataSet = GetSchemaDataSet(database);

            XElement databaseSchema = GetXml(database);

            return databaseSchema;
        }

        private static XElement GetXml(Database database)
        {
            XElement root = new(DATABASE_SCHEMA,
                new XAttribute(nameof(Database), database.Name),
                new XAttribute(nameof(database.Provider), database.Provider),
                new XAttribute(nameof(database.CreatedAt), database.CreatedAt));

            foreach (Table table in database.Tables.OrderBy(t => t.Type))
            {
                if (!string.IsNullOrWhiteSpace(table.Description))
                    root.Add(new XComment(" " + table.Description.Trim() + " "));

                XElement xTable = new(nameof(Table),
                    new XAttribute(nameof(table.Name), table.Name),
                    new XAttribute(nameof(table.Type), table.Type));

                if (table is View view)
                {
                    if (view.Updatable != null)
                        xTable.SetAttributeValue(nameof(view.Updatable), view.Updatable);
                }

                //
                Constraint? primaryKey = table.Constraints.SingleOrDefault(c => c is PrimaryKey);
                if (primaryKey != null)
                {
                    XElement xKey = new(PRIMARY_KEY,
                        new XAttribute(nameof(primaryKey.Name), primaryKey.Name));

                    foreach (string column in primaryKey.Columns)
                    {
                        xKey.Add(new XElement(COLUMN_REF,
                            new XAttribute(COLUMN_REF_NAME, column)));
                    }

                    xTable.Add(xKey);
                }

                //
                foreach (Column column in table.Columns)
                {

                    if (!string.IsNullOrWhiteSpace(column.Description))
                        xTable.Add(new XComment(" " + column.Description.Trim() + " "));

                    XElement xColumn = new(nameof(Column),
                        new XAttribute(nameof(column.Name), column.Name),
                        new XAttribute(nameof(column.DataType), column.DataType),
                        new XAttribute(nameof(column.Nullable), column.Nullable));

                    if (column.AutoIncrement != null)
                        xColumn.SetAttributeValue(nameof(column.AutoIncrement), column.AutoIncrement);

                    if (column.DefaultValue != null)
                        xColumn.SetAttributeValue(nameof(column.DefaultValue), column.DefaultValue);

                    if (column.MaxLength != null)
                        xColumn.SetAttributeValue(nameof(column.MaxLength), column.MaxLength);

                    if (column.FixedLength != null)
                        xColumn.SetAttributeValue(nameof(column.FixedLength), column.FixedLength);

                    if (column.Unicode != null)
                        xColumn.SetAttributeValue(nameof(column.Unicode), column.Unicode);

                    //  if (column.Collation != null)
                    //      xColumn.SetAttributeValue(nameof(column.Collation), column.Collation);
                    
                    if (column.Precision != null)
                        xColumn.SetAttributeValue(nameof(column.Precision), column.Precision);
                    
                    if (column.Scale != null)
                        xColumn.SetAttributeValue(nameof(column.Scale), column.Scale);

                    if (column.ConcurrencyMode != null)
                        xColumn.SetAttributeValue(nameof(column.ConcurrencyMode), column.ConcurrencyMode);

                    xTable.Add(xColumn);
                }

                // 
                foreach (UniqueConstraint unique in table.Constraints.Where(c => c is UniqueConstraint).Cast<UniqueConstraint>())
                {
                    XElement xUnique = new(nameof(UniqueConstraint),
                          new XAttribute(nameof(unique.Name), unique.Name));

                    foreach (string column in unique.Columns)
                    {
                        xUnique.Add(new XElement(COLUMN_REF,
                            new XAttribute(COLUMN_REF_NAME, column)));
                    }

                    xTable.Add(xUnique);
                }

                //
                foreach (CheckConstraint check in table.Constraints.Where(c => c is CheckConstraint).Cast<CheckConstraint>())
                {
                    XElement xCheck = new(nameof(CheckConstraint),
                        new XAttribute(nameof(check.Name), check.Name),
                        new XAttribute(nameof(check.BooleanExpression), check.BooleanExpression ?? check.Clause));

                    foreach (string column in check.Columns)
                    {
                        xCheck.Add(new XElement(COLUMN_REF,
                            new XAttribute(COLUMN_REF_NAME, column)));
                    }

                    xTable.Add(xCheck);
                }

                //
                foreach (ForeignKeyConstraint foreignKey in table.Constraints.Where(c => c is ForeignKeyConstraint).Cast<ForeignKeyConstraint>())
                {
                    XElement xForeignKey = new(nameof(ForeignKeyConstraint),
                        new XAttribute(nameof(foreignKey.Name), foreignKey.Name),
                        new XAttribute(FOREIGN_KEY_RELATED, foreignKey.RelatedTable));

                    string[] columns = [.. foreignKey.Columns];
                    string[] relatedColumns = [.. foreignKey.RelatedColumns];

                    //
                    System.Diagnostics.Debug.Assert(columns.Length == relatedColumns.Length);

                    for (int i = 0; i < columns.Length; i++)
                    {
                        xForeignKey.Add(new XElement(COLUMN_REF,
                            new XAttribute(COLUMN_REF_NAME, columns[i]),
                            new XAttribute(FOREIGN_KEY_RELATED, relatedColumns[i])));
                    }

                    xTable.Add(xForeignKey);
                }

                root.Add(xTable);
            }

            foreach (Sequence sequence in database.Sequences)
            {
                XElement xSequence = new(nameof(Sequence), new XAttribute(nameof(sequence.Name), sequence.Name));

                root.Add(xSequence);
            }

            return root;
        }

        private DataSet GetSchemaDataSet(Database database)
        {
            DataSet schemaDataSet = new(database.Name);

            string format = "SELECT * FROM " + database.LeftBracket + "{0}" + database.RightBracket;
            Connection.Open();
            try
            {
                foreach (Table table in database.Tables)
                {
                    DbCommand command = Connection.CreateCommand();
                    command.CommandText = string.Format(format, table.Name);
                    DbDataAdapter adapter = CreateDataAdapter();
                    adapter.SelectCommand = command;
                    adapter.FillSchema(schemaDataSet, SchemaType.Source, table.Name);
                }
            }
            finally
            {
                Connection.Close();
            }

            SetSchemaDataSet(schemaDataSet, database);

            return schemaDataSet;
        }

        private static void SetSchemaDataSet(DataSet schemaDataSet, Database database)
        {
            foreach (Table table in database.Tables)
            {
                DataTable? dataTable = schemaDataSet.Tables[table.Name];
                if (dataTable == null) continue;

                if (!string.IsNullOrWhiteSpace(table.Description))
                    dataTable.ExtendedProperties.Add(nameof(DataColumn.Caption), table.Description); // DataColumn.Caption has borrowed because DataTable does not have the property.

                var columns = table.Columns.Where(c => c.DefaultValue != null);
                if (!columns.Any()) continue;

                foreach (Column column in columns)
                {
                    DataColumn? dataColumn = dataTable.Columns[column.Name];
                    if (dataColumn == null) continue;

                    if (!string.IsNullOrWhiteSpace(column.Description))
                        dataColumn.Caption = column.Description;

                    if (column.DefaultValue == null) continue;

                    if (dataColumn.DataType == typeof(byte[])) continue;

                    if (dataColumn.DataType == typeof(Guid)) continue;

                    if (dataColumn.DataType == typeof(bool))
                    {
                        if (byte.TryParse(column.DefaultValue, out byte value))
                        {
                            dataColumn.DefaultValue = value > 0;
                        }
                    }
                    else
                    {
                        try
                        {
                            dataColumn.DefaultValue = Convert.ChangeType(column.DefaultValue, dataColumn.DataType);
                        }
                        catch
                        {
                            // do nothing
                        }
                    }
                }
            }
        }

        private static XElement GetXml(DataSet schemaDataSet)
        {
            using MemoryStream stream = new();
            schemaDataSet.WriteXmlSchema(stream);
            stream.Position = 0;
            XElement element = XElement.Load(stream);

            return element;
        }

        protected DataTable GetDataTable(string sql)
        {
            DbCommand command = Connection.CreateCommand();
            command.CommandText = sql;
            DbDataAdapter adapter = CreateDataAdapter();
            adapter.SelectCommand = command;
            DataTable table = new();
            adapter.Fill(table);
            return table;
        }

        protected const string STORE_SCHEMA = "StoreSchema";
        protected const string DATABASE_SCHEMA_COMMENT = " Database schema ";
        protected const string DATASET_SCHEMA_COMMENT = " DataSet schema ";
        protected const string DATABASE_SCHEMA = "Schema";
        protected const string PRIMARY_KEY = "Key";
        protected const string COLUMN_REF = "ColumnRef";
        protected const string COLUMN_REF_NAME = "Name";
        protected const string FOREIGN_KEY_RELATED = "Related";

        protected static string? ToNullableString(object data) => (data == DBNull.Value) ? null : (string)data;

        protected static Nullable<T> ToNullableType<T>(object obj) where T : struct
        {
            return (obj == DBNull.Value) ? null : (T)obj;
        }

    }
}
