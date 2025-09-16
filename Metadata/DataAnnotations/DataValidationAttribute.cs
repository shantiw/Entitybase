using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Shantiw.Data.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false)]
    public class DataValidationAttribute(string booleanExpression) : ValidationAttribute
    {
        public string BooleanExpression { get; private set; } = booleanExpression;

        public bool IsValid(string name, object value)
        {
            return IsValid(new Dictionary<string, object>() { { name, value } });
        }

        public bool IsValid(IEnumerable<KeyValuePair<string, object>> nameValues)
        {
            DataTable table = new();
            List<string> columnNames = [];
            foreach (var nameValue in (IEnumerable<KeyValuePair<string, object>>)nameValues)
            {
                columnNames.Add(nameValue.Key);

                DataColumn column = new()
                {
                    ColumnName = nameValue.Key,
                    DataType = nameValue.Value.GetType(),
                    DefaultValue = nameValue.Value
                };
                table.Columns.Add(column);
            }

            //
            string resultColumnName = "result";
            while (columnNames.Contains(resultColumnName))
            {
                resultColumnName += 1;
            }

            DataColumn resultColumn = new()
            {
                ColumnName = resultColumnName,
                DataType = typeof(bool),
                Expression = BooleanExpression
            };
            table.Columns.Add(resultColumn);

            DataRow row = table.NewRow();
            table.Rows.Add(row);

            return (bool)row[resultColumnName];
        }

        public override bool IsValid(object? value)
        {
            ArgumentNullException.ThrowIfNull(value, nameof(value));

            return IsValid((IEnumerable<KeyValuePair<string, object>>)value);
        }

    }
}
