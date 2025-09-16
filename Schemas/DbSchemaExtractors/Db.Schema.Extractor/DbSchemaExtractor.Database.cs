namespace Shantiw.Data.Schema
{
    public abstract partial class DbSchemaExtractor
    {
        protected class Database
        {
            public required string Provider { get; set; }
            public required string Name { get; set; }
            public required string LeftBracket { get; set; } // [name]
            public required string RightBracket { get; set; } // [name]
            public required DateTime CreatedAt { get; set; }
            public ICollection<Table> Tables { get; private set; } = [];
            public ICollection<Sequence> Sequences { get; private set; } = [];
        }

        protected class Table // INFORMATION_SCHEMA.TABLES
        {
            public required string Name { get; set; }  // TABLE_NAME
            public required TableType Type { get; set; } // "BASE TABLE" or "VIEW"
            public string? Description { get; set; }
            public ICollection<Column> Columns { get; set; } = [];
            public ICollection<Constraint> Constraints { get; set; } = [];
        }

        protected enum TableType { Table, View }

        protected class View : Table // INFORMATION_SCHEMA.VIEWS
        {
            public bool? Updatable { get; set; } // IS_UPDATABLE
        }

        protected class Column // INFORMATION_SCHEMA.COLUMNS
        {
            public required string Table { get; set; }
            public required string Name { get; set; } // COLUMN_NAME
            public required string DataType { get; set; } // DATA_TYPE, such as nchar, nvarchar...
            public required bool Nullable { get; set; } // IS_NULLABLE      
            public string? DefaultValue { get; set; } // COLUMN_DEFAULT
            public int? MaxLength { get; set; } // CHARACTER_MAXIMUM_LENGTH
            public bool? FixedLength { get; set; } // true: char(50), nchar(50)...; false: varchar(50), nvarchar(50)...               
            public int? Precision { get; set; } // NUMERIC_PRECISION
            public int? Scale { get; set; } //  NUMERIC_SCALE
            public bool? Unicode { get; set; } // CHARACTER_SET_NAME. true: nchar, nvarchar...; false: char, varchar...
            public string? Collation { get; set; } // COLLATION_NAME
            public ConcurrencyMode? ConcurrencyMode { get; set; }
            public bool? AutoIncrement { get; set; } // is_identity in [sys].[columns]
            public string? Description { get; set; }
        }

        protected enum ConcurrencyMode { None, Fixed }

        protected abstract class Constraint
        {
            public required string Table { get; set; }
            public required string Name { get; set; } // CONSTRAINT_NAME
            public ICollection<string> Columns { get; private set; } = []; // INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE
        }

        protected class PrimaryKey : Constraint
        {
        }

        protected class UniqueConstraint : Constraint
        {
        }

        protected class CheckConstraint : Constraint// INFORMATION_SCHEMA.CHECK_CONSTRAINTS
        {
            public required string Clause { get; set; } // CHECK_CLAUSE
            public string? BooleanExpression { get; set; } // boolean expression
        }

        protected class ForeignKeyConstraint : Constraint // INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS
        {
            public required string RelatedTable { get; set; }
            public ICollection<string> RelatedColumns { get; private set; } = [];
        }

        protected class Sequence // INFORMATION_SCHEMA.SEQUENCES
        {
            public required string Name { get; set; } // SEQUENCE_NAME
        }

    }
}
