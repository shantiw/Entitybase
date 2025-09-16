using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shantiw.Data.Schema
{
    public interface IMapper
    {
        public abstract bool IsGeneratingDisplayAttrForEntityTypeByCommnent { get; }
        public abstract bool IsGeneratingDisplayAttrForPropertyByCommnent { get; }

        public abstract (string entityTypeName, string entitySetName) GetEntityTypeName(string tableName);

        public abstract string GetPropertyName(string columnName, string tableName);

        public abstract string? GetSequenceName(string columnName, string tableName);
    }
}
