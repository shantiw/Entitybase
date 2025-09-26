using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shantiw.Data.Schema
{
    [Flags]
    public enum CommnentPolicy
    {
        None = 0,
        DisplayName = 1,
        Description = 2
    }

    public interface IMapper
    {
        public abstract CommnentPolicy EntityTypeCommnentPolicy { get; }
        public abstract CommnentPolicy PropertyCommnentPolicy { get; }

        public abstract (string entityTypeName, string entitySetName) GetEntityTypeName(string tableName);

        public abstract string GetPropertyName(string columnName, string tableName);

        public abstract string? GetSequenceName(string columnName, string tableName);
    }
}
