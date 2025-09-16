using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shantiw.Data.Schema
{
    internal static class ConfigMapperVocab
    {
        public const string Name = "Name";
        public const string Format = "Format";

        public const string Functions = "Functions";
        public const string Parameter = "Parameter";
        public const string Function = "Function";
        public const string Order = "Order";

        public const string TableNamePlaceholder = "{TableName}";
        public const string ColumnNamePlaceholder = "{ColumnName}";
        public const string FuncsTableNamePlaceholder = "{Funcs(TableName)}";
        public const string FuncsColumnNamePlaceholder = "{Funcs(ColumnName)}";
        public const string NullPlaceholder = "{Null}"; // SequenceName

        public const string TableName = "TableName";
        public const string ColumnName = "ColumnName";

    }
}
