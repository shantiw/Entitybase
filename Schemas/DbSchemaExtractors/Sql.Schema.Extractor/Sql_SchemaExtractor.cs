using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;

namespace Shantiw.Data.Schema
{
    internal class Sql_SchemaExtractor(string connectionString) : DbSchemaExtractor(connectionString)
    {
        private const string PROVIDER = "Microsoft.Data.SqlClient";

        protected override DbConnection CreateConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        protected override DbDataAdapter CreateDataAdapter()
        {
            return new SqlDataAdapter();
        }

        protected override Database GetDatabase() // exclude from sysdiagrams
        {
            string sql = "SELECT DB_NAME(), SYSUTCDATETIME()";
            DataTable dataTable = GetDataTable(sql);
            Database database = new()
            {
                Name = (string)dataTable.Rows[0][0],
                Provider = PROVIDER,
                CreatedAt = (DateTime)dataTable.Rows[0][1],
                LeftBracket = "[",
                RightBracket = "]"
            };

            SetTables(database);

            SetColumns(database);

            SetConstraints(database);

            SetSequences(database);

            SetAutoIncrement(database);

            SetDescriptions(database);

            return database;
        }

        private void SetTables(Database database)
        {
            string sql = @"
SELECT T.[TABLE_NAME]
      ,T.[TABLE_TYPE]
	  ,V.[IS_UPDATABLE]
  FROM [INFORMATION_SCHEMA].[TABLES] T
  LEFT JOIN [INFORMATION_SCHEMA].[VIEWS] V
  ON T.[TABLE_NAME] = V.[TABLE_NAME]
  WHERE T.[TABLE_NAME] != N'sysdiagrams'
";
            DataTable dataTable = GetDataTable(sql);
            foreach (DataRow row in dataTable.Rows)
            {
                string tableName = (string)row[0];
                string tableType = (string)row[1];
                Table table = (tableType == "VIEW") ?
                    new View() { Name = tableName, Type = TableType.View, Updatable = (string)row[2] == "YES" } :
                    new Table() { Name = tableName, Type = TableType.Table };
                database.Tables.Add(table);
            }
        }

        private void SetColumns(Database database)
        {
            List<Column> columns = [];
            string sql = @"
SELECT [TABLE_NAME]
      ,[COLUMN_NAME]
      ,[ORDINAL_POSITION]
      ,[COLUMN_DEFAULT]
      ,[IS_NULLABLE]
      ,[DATA_TYPE]
      ,[CHARACTER_MAXIMUM_LENGTH]
      ,[NUMERIC_PRECISION]
      ,[NUMERIC_SCALE]
      ,[DATETIME_PRECISION]
      ,[CHARACTER_SET_CATALOG]
      ,[CHARACTER_SET_SCHEMA]
      ,[CHARACTER_SET_NAME]
      ,[COLLATION_CATALOG]
      ,[COLLATION_SCHEMA]
      ,[COLLATION_NAME]
  FROM [INFORMATION_SCHEMA].[COLUMNS]
  WHERE[TABLE_NAME] != N'sysdiagrams'
";
            DataTable dataTable = GetDataTable(sql);
            foreach (DataRow row in dataTable.Rows)
            {
                Column column = new()
                {
                    Name = (string)row["COLUMN_NAME"],
                    Table = (string)row["TABLE_NAME"],
                    DataType = (string)row["DATA_TYPE"],
                    Nullable = (string)row["IS_NULLABLE"] == "YES",
                    MaxLength = ToNullableType<int>(row["CHARACTER_MAXIMUM_LENGTH"])
                };
                string? defaultValue = ToNullableString(row["COLUMN_DEFAULT"]);
                if (defaultValue != null)
                {
                    if (defaultValue.StartsWith("('") && defaultValue.EndsWith("')"))
                    {
                        defaultValue = defaultValue[2..^2];
                    }
                    else if (defaultValue.StartsWith("(N'") && defaultValue.EndsWith("')"))
                    {
                        defaultValue = defaultValue[3..^2];
                    }
                    else if (defaultValue.StartsWith("((") && defaultValue.EndsWith("))"))
                    {
                        defaultValue = defaultValue.TrimStart('(').TrimEnd(')');
                    }
                    else if (defaultValue.StartsWith('(') && defaultValue.EndsWith(')'))
                    {
                        defaultValue = defaultValue[1..^1];
                    }
                }
                column.DefaultValue = defaultValue;

                column.FixedLength = !(column.DataType.StartsWith("var") || column.DataType.StartsWith("nvar") ||
                                       column.DataType == "text" || column.DataType == "ntext" ||
                                       column.DataType == "image" || column.DataType == "xml");

                string? unicode = ToNullableString(row["CHARACTER_SET_SCHEMA"]);
                column.Unicode = (unicode == null) ? null : (unicode == "UNICODE");
                column.Collation = ToNullableString(row["COLLATION_NAME"]);
                column.Precision = ToNullableType<byte>(row["NUMERIC_PRECISION"]);
                column.Precision = (column.Precision == null) ? ToNullableType<short>(row["DATETIME_PRECISION"]) : column.Precision;
                column.Scale = ToNullableType<int>(row["NUMERIC_SCALE"]);
                if (column.DataType == "timestamp" || column.DataType == "rowversion") column.ConcurrencyMode = ConcurrencyMode.Fixed;
                columns.Add(column);
            }
            foreach (Table table in database.Tables)
            {
                table.Columns = [.. columns.Where(c => c.Table == table.Name)];
            }
        }

        private void SetConstraints(Database database)
        {
            // PrimaryKey, UniqueConstraint and CHECK
            string sql = @"
SELECT T.[CONSTRAINT_NAME]
      ,T.[TABLE_NAME]
      ,T.[CONSTRAINT_TYPE]
	  ,C.[CHECK_CLAUSE]
  FROM [INFORMATION_SCHEMA].[TABLE_CONSTRAINTS] T
  LEFT JOIN [INFORMATION_SCHEMA].[CHECK_CONSTRAINTS] C 
  ON T.[CONSTRAINT_NAME] = C.[CONSTRAINT_NAME]
  WHERE T.[CONSTRAINT_TYPE] != 'FOREIGN KEY'
  AND T.[TABLE_NAME] != N'sysdiagrams'
";
            DataTable dataTable = GetDataTable(sql);
            foreach (DataRow row in dataTable.Rows)
            {
                Table table = database.Tables.Single(t => t.Name == (string)row["TABLE_NAME"]);
                string name = (string)row["CONSTRAINT_NAME"];
                string type = (string)row["CONSTRAINT_TYPE"];
                switch (type)
                {
                    case "PRIMARY KEY":
                        table.Constraints.Add(new PrimaryKey() { Name = name, Table = table.Name });
                        break;
                    case "UNIQUE":
                        table.Constraints.Add(new UniqueConstraint() { Name = name, Table = table.Name });
                        break;
                    case "CHECK":
                        table.Constraints.Add(new CheckConstraint() { Name = name, Table = table.Name, Clause = (string)row["CHECK_CLAUSE"] });
                        break;
                }
            }

            //
            sql = @"
SELECT U.[TABLE_NAME]
      ,U.[COLUMN_NAME]
      ,U.[CONSTRAINT_NAME]
  FROM [INFORMATION_SCHEMA].[CONSTRAINT_COLUMN_USAGE] U
  LEFT JOIN [INFORMATION_SCHEMA].[TABLE_CONSTRAINTS] T
  ON U.[TABLE_NAME] = T.[TABLE_NAME]
  AND U.[CONSTRAINT_NAME] = T.[CONSTRAINT_NAME]
  WHERE T.[CONSTRAINT_TYPE] !='FOREIGN KEY'
  AND U.[TABLE_NAME] != N'sysdiagrams'
";
            dataTable = GetDataTable(sql);
            foreach (DataRow row in dataTable.Rows)
            {
                Table table = database.Tables.Single(t => t.Name == (string)row["TABLE_NAME"]);
                Constraint constraint = table.Constraints.First(c => c.Name == (string)row["CONSTRAINT_NAME"]);
                constraint.Columns.Add((string)row["COLUMN_NAME"]);
            }

            //
            SetForeignKeys(database);
        }

        protected virtual void SetForeignKeys(Database database) // Has a bug. [INFORMATION_SCHEMA].[KEY_COLUMN_USAGE] does not include UniqueKey
        {
            Dictionary<string, ForeignKeyConstraint> nameFKPairs = [];

            string sql = @"
SELECT R.[CONSTRAINT_NAME]
      ,R.[UNIQUE_CONSTRAINT_NAME]
      ,K.[TABLE_NAME]
	  ,K.[COLUMN_NAME]
	  ,K.[ORDINAL_POSITION]
	  ,C.[TABLE_NAME] AS R_TABLE_NAME
	  ,C.[COLUMN_NAME] AS R_COLUMN_NAME
  FROM [INFORMATION_SCHEMA].[REFERENTIAL_CONSTRAINTS] R
  INNER JOIN [INFORMATION_SCHEMA].[KEY_COLUMN_USAGE] K
  ON R.[CONSTRAINT_NAME] = K.[CONSTRAINT_NAME]
  INNER JOIN [INFORMATION_SCHEMA].[KEY_COLUMN_USAGE] C
  ON R.[UNIQUE_CONSTRAINT_NAME] = C.[CONSTRAINT_NAME]
  AND K.[ORDINAL_POSITION] = C.[ORDINAL_POSITION]
  WHERE K.[TABLE_NAME] != N'sysdiagrams' AND C.[TABLE_NAME] != N'sysdiagrams'
  ORDER BY R.[CONSTRAINT_NAME], K.[ORDINAL_POSITION]
";
            DataTable dataTable = GetDataTable(sql);
            foreach (DataRow row in dataTable.Rows)
            {
                string name = (string)row["CONSTRAINT_NAME"];
                if (!nameFKPairs.TryGetValue(name, out ForeignKeyConstraint? value))
                {
                    value = new ForeignKeyConstraint() { Name = name, Table = (string)row["TABLE_NAME"], RelatedTable = (string)row["R_TABLE_NAME"] };
                    nameFKPairs[name] = value;
                }
                value.Columns.Add((string)row["COLUMN_NAME"]);
                value.RelatedColumns.Add((string)row["R_COLUMN_NAME"]);
            }

            foreach (ForeignKeyConstraint foreignKey in nameFKPairs.Values)
            {
                Table table = database.Tables.Single(t => t.Name == foreignKey.Table);
                table.Constraints.Add(foreignKey);
            }
        }

        private void SetSequences(Database database)
        {
            string sql = @"
SELECT [SEQUENCE_NAME]
      ,[DATA_TYPE]
      ,[NUMERIC_PRECISION]
      ,[NUMERIC_PRECISION_RADIX]
      ,[NUMERIC_SCALE]
      ,[START_VALUE]
      ,[MINIMUM_VALUE]
      ,[MAXIMUM_VALUE]
      ,[INCREMENT]
      ,[CYCLE_OPTION]
      ,[DECLARED_DATA_TYPE]
      ,[DECLARED_NUMERIC_PRECISION]
      ,[DECLARED_NUMERIC_SCALE]
  FROM [INFORMATION_SCHEMA].[SEQUENCES]
";
            DataTable dataTable = GetDataTable(sql);
            foreach (DataRow row in dataTable.Rows)
            {
                database.Sequences.Add(new Sequence() { Name = (string)row["SEQUENCE_NAME"] });
            }
        }

        private void SetAutoIncrement(Database database)
        {
            string sql = @"
SELECT t.[type] AS table_type
	  ,t.[type_desc] AS table_type_desc
	  ,t.[name] AS table_name
      ,c.[name] AS column_name
  FROM [sys].[columns] c
  INNER JOIN [sys].[objects] t 
  ON c.[object_id] = t.[object_id]
  AND t.[type] = N'U'
  AND c.[is_identity] = 1
";
            DataTable dataTable = GetDataTable(sql);
            foreach (DataRow row in dataTable.Rows)
            {
                Table? table = database.Tables.FirstOrDefault(t => t.Name == (string)row["table_name"]);
                if (table != null)
                {
                    table.Columns.Single(c => c.Name == (string)row["column_name"]).AutoIncrement = true;
                }
            }
        }

        private void SetDescriptions(Database database) // description of the table or column
        {
            string sql = @"
SELECT t.[name] AS table_name
	  ,c.[name] AS column_name
      ,p.[value]
  FROM [sys].[extended_properties] p
  INNER JOIN [sys].[objects] t 
  ON p.[major_id] = t.[object_id]
  LEFT JOIN [sys].[columns] c
  ON p.[major_id] = c.[object_id] AND p.[minor_id] = c.[column_id]
  WHERE p.[name] = N'MS_Description'
";
            DataTable dataTable = GetDataTable(sql);
            foreach (DataRow row in dataTable.Rows)
            {        
                Table? table = database.Tables.FirstOrDefault(t => t.Name == (string)row["table_name"]);
                if (table != null)
                {
                    string description = (string)row["value"];
                    object column_name = row["column_name"];
                    if (column_name == DBNull.Value)
                    {
                        table.Description = description;
                    }
                    else
                    {
                        Column column = table.Columns.Single(c => c.Name == (string)column_name);
                        column.Description = description;
                    }
                }
            }
        }

    }
}