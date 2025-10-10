using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shantiw.Data.Schema
{
    internal static class DbSchemaVocab // DatabaseSchemaVocabulary
    {
        public const string Name = "Name";

        public const string Schema = "Schema";
        public const string Database = "Database";
        public const string Provider = "Provider";
        public const string CreatedAt = "CreatedAt";

        public const string Table = "Table";
        public const string TableType = "Type";
        public const string ViewUpdatable = "Updatable";

        public const string Column = "Column";
        public const string Nullable = "Nullable";
        public const string DataType = "DataType";
        public const string AutoIncrement = "AutoIncrement";
        public const string DefaultValue = "DefaultValue";
        public const string MaxLength = "MaxLength";
        public const string FixedLength = "FixedLength";
        public const string Unicode = "Unicode";
        public const string Precision = "Precision";
        public const string Scale = "Scale";
        public const string Collation = "Collation";
        public const string ConcurrencyMode = "ConcurrencyMode";
        public const string ConcurrencyModeNone = "None";
        public const string ConcurrencyModeFixed = "Fixed";

        public const string Key = "Key";

        public const string Unique = "UniqueConstraint";

        public const string Check = "CheckConstraint";
        public const string Clause = "Clause";

        public const string ForeignKey = "ForeignKeyConstraint";
        public const string RelatedTable = "Related";
        public const string RelatedColumn = "Related";

        public const string ColumnRef = "ColumnRef";

        public const string Sequence = "Sequence";

    }
}
