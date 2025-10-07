using Microsoft.VisualBasic;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shantiw.Data.Schema
{
    // MySQL 5.7
    // Check constraint does not work.
    // Sequences is not supported.
    internal class MySqlSchemaExtractor(string connectionString) : DbSchemaExtractor(connectionString)
    {
        private const string PROVIDER = "MySql.Data.MySqlClient";

        protected override DbConnection CreateConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        protected override DbDataAdapter CreateDataAdapter()
        {
            return new MySqlDataAdapter();
        }

        protected override Database GetDatabase()
        {
            string sql = "SELECT DATABASE(), UTC_TIMESTAMP()";
            DataTable dataTable = GetDataTable(sql);
            Database database = new()
            {
                Name = (string)dataTable.Rows[0][0],
                Provider = PROVIDER,
                CreatedAt = (DateTime)dataTable.Rows[0][1],
                LeftBracket = "`",
                RightBracket = "`"
            };

            SetTables(database);

            SetColumns(database);

            SetConstraints(database);

            return database;
        }

        private void SetTables(Database database)
        {
            string sql = @"
SELECT 
 t.`TABLE_NAME`,
 t.`TABLE_TYPE`,
 t.`TABLE_COMMENT`,
 v.`IS_UPDATABLE`
 FROM information_schema.TABLES t
 LEFT JOIN information_schema.VIEWS V
 ON t.`TABLE_NAME` = v.`TABLE_NAME`
 WHERE t.TABLE_SCHEMA = '{0}'
";
            sql = string.Format(sql, database.Name);
            DataTable dataTable = GetDataTable(sql);
            foreach (DataRow row in dataTable.Rows)
            {
                string tableName = (string)row[0];
                string tableType = (string)row[1];
                Table table = (tableType == "VIEW") ?
                    new View() { Name = tableName, Type = TableType.View, Updatable = (string)row[3] == "YES" } :
                    new Table() { Name = tableName, Type = TableType.Table };
                table.Description = ToNullableString(row[2]);
                database.Tables.Add(table);
            }
        }

        private void SetColumns(Database database)
        {
            List<Column> columns = [];
            string sql = @"
SELECT 
 `TABLE_NAME`, 
 `COLUMN_NAME`,  
 `COLUMN_DEFAULT`, 
 `IS_NULLABLE`,
 `DATA_TYPE`, 
 `CHARACTER_MAXIMUM_LENGTH`, 
 `NUMERIC_PRECISION`, 
 `NUMERIC_SCALE`, 
 `DATETIME_PRECISION`, 
 `CHARACTER_SET_NAME`, 
 `COLLATION_NAME`,
 `EXTRA`, 
 `COLUMN_COMMENT`
 FROM information_schema.COLUMNS
 WHERE `TABLE_SCHEMA` = '{0}'
 ORDER BY `TABLE_NAME`, `ORDINAL_POSITION`
";
            sql = string.Format(sql, database.Name);
            DataTable dataTable = GetDataTable(sql);
            foreach (DataRow row in dataTable.Rows)
            {
                Column column = new()
                {
                    Name = (string)row["COLUMN_NAME"],
                    Table = (string)row["TABLE_NAME"],
                    DataType = (string)row["DATA_TYPE"],
                    Nullable = (string)row["IS_NULLABLE"] == "YES",
                    DefaultValue = ToNullableString(row["COLUMN_DEFAULT"]),
                    MaxLength = (int?)ToNullableType<ulong>(row["CHARACTER_MAXIMUM_LENGTH"])
                };

                column.FixedLength = !(column.DataType.StartsWith("var") || column.DataType.StartsWith("nvar") ||
                                       column.DataType.Contains("blob") || column.DataType.Contains("text") ||
                                       column.DataType == "json");

                string? unicode = ToNullableString(row["CHARACTER_SET_NAME"]);
                column.Unicode = (unicode?.StartsWith("utf"));
                column.Precision = (int?)ToNullableType<ulong>(row["NUMERIC_PRECISION"]);
                column.Precision = (column.Precision == null) ? (int?)ToNullableType<ulong>(row["DATETIME_PRECISION"]) : column.Precision;
                column.Scale = (int?)ToNullableType<ulong>(row["NUMERIC_SCALE"]);
                column.Collation = ToNullableString(row["COLLATION_NAME"]);
                string? extra = ToNullableString(row["EXTRA"]);
                column.AutoIncrement = extra == "auto_increment";
                column.Description = ToNullableString(row["COLUMN_COMMENT"]);
                if (extra == "on update CURRENT_TIMESTAMP" && column.DefaultValue == "CURRENT_TIMESTAMP") column.ConcurrencyMode = ConcurrencyMode.Fixed;
                columns.Add(column);
            }
            foreach (Table table in database.Tables)
            {
                table.Columns = [.. columns.Where(c => c.Table == table.Name)];
            }
        }

        private void SetConstraints(Database database)
        {
            string sql = @"
SELECT
 u.`CONSTRAINT_NAME`,
 u.`TABLE_NAME`,
 u.`COLUMN_NAME`,
 u.`ORDINAL_POSITION`,
 t.`CONSTRAINT_TYPE`
 FROM information_schema.KEY_COLUMN_USAGE u
 LEFT JOIN information_schema.TABLE_CONSTRAINTS t
 ON u.`TABLE_NAME` = t.`TABLE_NAME`
 AND u.`CONSTRAINT_NAME` = t.`CONSTRAINT_NAME`
 WHERE t.`CONSTRAINT_TYPE` != 'FOREIGN KEY'
 AND u.`TABLE_SCHEMA` = '{0}'
 ORDER BY u.`TABLE_NAME`, u.`ORDINAL_POSITION`
";
            sql = string.Format(sql, database.Name);
            DataTable dataTable = GetDataTable(sql);
            foreach (DataRow row in dataTable.Rows)
            {
                Table table = database.Tables.Single(t => t.Name == (string)row["TABLE_NAME"]);
                string name = (string)row["CONSTRAINT_NAME"];
                Constraint? constraint = table.Constraints.SingleOrDefault(c => c.Name == name);
                if (constraint == null)
                {
                    string type = (string)row["CONSTRAINT_TYPE"];
                    constraint = type switch
                    {
                        "PRIMARY KEY" => new PrimaryKey() { Name = name, Table = table.Name },
                        "UNIQUE" => new UniqueConstraint() { Name = name, Table = table.Name },
                        _ => throw new NotSupportedException(type + " CONSTRAINT"),
                    };
                    table.Constraints.Add(constraint);
                }
                constraint.Columns.Add((string)row["COLUMN_NAME"]);
            }

            //
            SetForeignKeys(database);
        }

        private void SetForeignKeys(Database database)
        {
            string sql = @"
SELECT
 u.`CONSTRAINT_NAME`,
 u.`TABLE_NAME`, 
 u.`COLUMN_NAME`, 
 u.POSITION_IN_UNIQUE_CONSTRAINT, 
 u.REFERENCED_TABLE_NAME, 
 u.REFERENCED_COLUMN_NAME
 FROM information_schema.KEY_COLUMN_USAGE u
 LEFT JOIN information_schema.TABLE_CONSTRAINTS t
 ON u.`TABLE_NAME` = t.`TABLE_NAME`
 AND u.`CONSTRAINT_NAME` = t.`CONSTRAINT_NAME`
 WHERE t.CONSTRAINT_TYPE = 'FOREIGN KEY' 
 AND u.`TABLE_SCHEMA` = '{0}'
 ORDER BY u.`TABLE_NAME`, u.`CONSTRAINT_NAME`, u.POSITION_IN_UNIQUE_CONSTRAINT
";
            sql = string.Format(sql, database.Name);
            DataTable dataTable = GetDataTable(sql);
            foreach (DataRow row in dataTable.Rows)
            {
                Table table = database.Tables.Single(t => t.Name == (string)row["TABLE_NAME"]);
                string name = (string)row["CONSTRAINT_NAME"];
                ForeignKeyConstraint? foreignKey = (ForeignKeyConstraint?)table.Constraints.SingleOrDefault(c => c.Name == name);
                if (foreignKey == null)
                {
                    foreignKey = new ForeignKeyConstraint() { Name = name, Table = table.Name, RelatedTable = (string)row["REFERENCED_TABLE_NAME"] };
                    table.Constraints.Add(foreignKey);
                }
                foreignKey.Columns.Add((string)row["COLUMN_NAME"]);
                foreignKey.RelatedColumns.Add((string)row["REFERENCED_COLUMN_NAME"]);
            }
        }

    }
}
