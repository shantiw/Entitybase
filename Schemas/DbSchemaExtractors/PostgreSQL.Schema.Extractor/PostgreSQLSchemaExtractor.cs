using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shantiw.Data.Schema
{
    internal class PostgreSQLSchemaExtractor(string connectionString) : DbSchemaExtractor(connectionString)
    {
        private const string PROVIDER = "Npgsql";

        protected override DbConnection CreateConnection()
        {
            return new NpgsqlConnection(ConnectionString);
        }

        protected override DbDataAdapter CreateDataAdapter()
        {
            return new NpgsqlDataAdapter();
        }

        protected override Database CreateDatabase()
        {
            string sql = "SELECT current_database(), CURRENT_TIMESTAMP, current_schema()";
            DataTable dataTable = CreateDataTable(sql);
            Database database = new()
            {
                Name = (string)dataTable.Rows[0][0],
                Provider = PROVIDER,
                CreatedAt = (DateTime)dataTable.Rows[0][1],
                LeftBracket = "\"",
                RightBracket = "\""
            };

            string currentSchema = (string)dataTable.Rows[0][2];

            SetTables(database, currentSchema);

            SetColumns(database, currentSchema);

            SetConstraints(database, currentSchema);

            SetForeignKeys(database, currentSchema);

            SetSequences(database, currentSchema);

            SetDescriptions(database, currentSchema);

            return database;
        }

        private void SetTables(Database database, string currentSchema)
        {
            string sql = @"
SELECT t.table_name, t.table_type, v.is_updatable
 FROM information_schema.tables t
 LEFT JOIN information_schema.views v
 ON t.table_name = v.table_name
 WHERE t.table_schema = '{0}'
";
            sql = string.Format(sql, currentSchema);
            DataTable dataTable = CreateDataTable(sql);
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

        private void SetColumns(Database database, string currentSchema)
        {
            List<Column> columns = [];
            string sql = @"
SELECT
 table_name,
 column_name,
 ordinal_position,
 column_default,
 is_nullable,
 data_type,
 character_maximum_length,
 numeric_precision,
 numeric_scale,
 datetime_precision,
 interval_precision,
 character_set_name,
 collation_name
 FROM information_schema.columns
 WHERE table_schema = '{0}'
 ORDER BY table_name, ordinal_position
";
            sql = string.Format(sql, currentSchema);
            DataTable dataTable = CreateDataTable(sql);
            foreach (DataRow row in dataTable.Rows)
            {
                Column column = new()
                {
                    Name = (string)row["column_name"],
                    Table = (string)row["table_name"],
                    DataType = (string)row["data_type"],
                    Nullable = (string)row["is_nullable"] == "YES",
                    DefaultValue = ToNullableString(row["column_default"]),
                    MaxLength = (int?)ToNullableType<int>(row["character_maximum_length"])
                };

                column.FixedLength = !(column.DataType.Contains("varying") || column.DataType == "text" ||
                                  column.DataType == "json" || column.DataType == "xml");

                string? unicode = ToNullableString(row["character_set_name"]);
                column.Unicode = (unicode?.StartsWith("UTF"));

                column.Precision = (int?)ToNullableType<int>(row["numeric_precision"]);
                column.Precision = (column.Precision == null) ? (int?)ToNullableType<int>(row["datetime_precision"]) : column.Precision;
                column.Precision = (column.Precision == null) ? (int?)ToNullableType<int>(row["interval_precision"]) : column.Precision;

                column.Scale = (int?)ToNullableType<int>(row["numeric_scale"]);
                column.Collation = ToNullableString(row["collation_name"]);

                column.AutoIncrement = column.DataType.Contains("serial") || column.DefaultValue != null && column.DefaultValue.Contains("nextval");

                columns.Add(column);
            }

            foreach (Table table in database.Tables)
            {
                table.Columns = [.. columns.Where(c => c.Table == table.Name)];
            }
        }

        private void SetConstraints(Database database, string currentSchema)
        {
            // PrimaryKey, UniqueConstraint
            string sql = @"
SELECT k.constraint_name, k.table_name, k.column_name,
 k.ordinal_position, t.constraint_type
 FROM information_schema.key_column_usage k
 INNER JOIN information_schema.table_constraints t
 ON k.constraint_name = t.constraint_name
 WHERE k.constraint_schema = '{0}'
 AND k.position_in_unique_constraint IS NULL
 ORDER BY k.table_name, k.ordinal_position
";
            sql = string.Format(sql, currentSchema);
            DataTable dataTable = CreateDataTable(sql);
            foreach (DataRow row in dataTable.Rows)
            {
                Table table = database.Tables.Single(t => t.Name == (string)row["table_name"]);
                string name = (string)row["constraint_name"];
                Constraint? constraint = table.Constraints.SingleOrDefault(c => c.Name == name);
                if (constraint == null)
                {
                    string type = (string)row["constraint_type"];
                    constraint = type switch
                    {
                        "PRIMARY KEY" => new PrimaryKey() { Name = name, Table = table.Name },
                        "UNIQUE" => new UniqueConstraint() { Name = name, Table = table.Name },
                        _ => throw new NotSupportedException(type + " CONSTRAINT"),
                    };
                    table.Constraints.Add(constraint);
                }
                constraint.Columns.Add((string)row["column_name"]);
            }

            // CheckConstraint
            sql = @"
SELECT c.table_name, c.column_name, c.constraint_name, ch.check_clause
 FROM information_schema.constraint_column_usage c
 INNER JOIN information_schema.check_constraints ch
 ON c.constraint_name = ch.constraint_name
 WHERE c.constraint_schema = '{0}'
";
            sql = string.Format(sql, currentSchema);
            dataTable = CreateDataTable(sql);
            foreach (DataRow row in dataTable.Rows)
            {
                Table table = database.Tables.Single(t => t.Name == (string)row["table_name"]);
                string name = (string)row["constraint_name"];
                Constraint? constraint = table.Constraints.SingleOrDefault(c => c.Name == name);
                if (constraint == null)
                {
                    constraint = new CheckConstraint() { Name = name, Table = table.Name, Clause = (string)row["check_clause"] };
                    table.Constraints.Add(constraint);
                }
                constraint.Columns.Add((string)row["column_name"]);
            }
        }

        private void SetForeignKeys(Database database, string currentSchema)
        {
            string sql = @"
SELECT r.constraint_name, r.unique_constraint_name,
 k.table_name, k.column_name, k.ordinal_position, k.position_in_unique_constraint,
 U.table_name AS unique_table_name, U.column_name AS unique_column_name
 FROM information_schema.referential_constraints r
 INNER JOIN information_schema.key_column_usage k
 ON r.constraint_name = k.constraint_name
 INNER JOIN information_schema.key_column_usage u
 ON r.unique_constraint_name = u.constraint_name
 AND k.position_in_unique_constraint = u.ordinal_position
 WHERE r.constraint_schema = '{0}'
";
            sql = string.Format(sql, currentSchema);
            DataTable dataTable = CreateDataTable(sql);
            foreach (DataRow row in dataTable.Rows)
            {
                Table table = database.Tables.Single(t => t.Name == (string)row["table_name"]);
                string name = (string)row["constraint_name"];
                ForeignKeyConstraint? foreignKey = (ForeignKeyConstraint?)table.Constraints.SingleOrDefault(c => c.Name == name);
                if (foreignKey == null)
                {
                    foreignKey = new ForeignKeyConstraint() { Name = name, Table = table.Name, RelatedTable = (string)row["unique_table_name"] };
                    table.Constraints.Add(foreignKey);
                }
                foreignKey.Columns.Add((string)row["column_name"]);
                foreignKey.RelatedColumns.Add((string)row["unique_column_name"]);
            }
        }

        private void SetSequences(Database database, string currentSchema)
        {
            string sql = @"
SELECT sequence_name, data_type
 FROM information_schema.""sequences""
 WHERE sequence_schema = '{0}'
";
            sql = string.Format(sql, currentSchema);
            DataTable dataTable = CreateDataTable(sql);
            foreach (DataRow row in dataTable.Rows)
            {
                database.Sequences.Add(new Sequence() { Name = (string)row["sequence_name"] });
            }
        }

        private void SetDescriptions(Database database, string currentSchema)
        {
            string sql = @"
SELECT d.description, c.relname, a.attname
 FROM pg_description d
 INNER JOIN pg_class c
 ON d.objoid = c.oid
 LEFT JOIN pg_attribute a
 ON d.objoid = a.attrelid
 AND d.objsubid = a.attnum
 WHERE c.relnamespace =
 (SELECT oid FROM pg_namespace WHERE nspname = '{0}')
";
            sql = string.Format(sql, currentSchema);
            DataTable dataTable = CreateDataTable(sql);
            foreach (DataRow row in dataTable.Rows)
            {
                Table? table = database.Tables.FirstOrDefault(t => t.Name == (string)row["relname"]);
                if (table != null)
                {
                    string description = (string)row["description"];
                    object column_name = row["attname"];
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
