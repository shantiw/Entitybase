using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shantiw.Data.Schema
{
    internal class SqlSchemaExtractor(string connectionString) : Sql_SchemaExtractor(connectionString)
    {
        protected override void SetForeignKeys(Database database) // a patch to fix the bug
        {
            Dictionary<string, ForeignKeyConstraint> nameFKPairs = [];

            string sql = @"
SELECT f.[name]
	  ,t1.[name] AS parent_table
	  ,c1.[name] AS parent_column
      ,t2.[name] AS referenced_table
	  ,c2.[name] AS referenced_column
  FROM [sys].[foreign_keys] f
  INNER JOIN [sys].[foreign_key_columns] c
  ON f.object_id = c.constraint_object_id
  INNER JOIN [sys].[tables] t1
  ON f.[parent_object_id] = t1.object_id
  INNER JOIN [sys].[tables] t2
  ON f.[referenced_object_id] = t2.object_id
  INNER JOIN [sys].[columns] c1
  ON c1.object_id =  f.[parent_object_id]
  AND c.[parent_column_id] = c1.column_id
  INNER JOIN [sys].[columns] c2
  ON c2.object_id =  f.[referenced_object_id]
  AND c.[referenced_column_id] = c2.column_id
  ORDER BY f.[name]
";
            DataTable dataTable = CreateDataTable(sql);
            foreach (DataRow row in dataTable.Rows)
            {
                string name = (string)row["name"];
                if (!nameFKPairs.TryGetValue(name, out ForeignKeyConstraint? value))
                {
                    value = new ForeignKeyConstraint() { Name = name, Table = (string)row["parent_table"], RelatedTable = (string)row["referenced_table"] };
                    nameFKPairs[name] = value;
                }
                value.Columns.Add((string)row["parent_column"]);
                value.RelatedColumns.Add((string)row["referenced_column"]);
            }

            foreach (ForeignKeyConstraint foreignKey in nameFKPairs.Values)
            {
                Table table = database.Tables.Single(t => t.Name == foreignKey.Table);
                table.Constraints.Add(foreignKey);
            }
        }

    }
}
