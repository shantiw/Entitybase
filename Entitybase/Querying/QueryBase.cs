using Shantiw.Data.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shantiw.Data.Querying
{
    internal static class QueryVocab
    {
        public const string EntitySet = "EntitySet";
        public const string Select = "Select";
        public const string Orderby = "Orderby";
        public const string Expand = "Expand";
    }

    internal class Filter
    {
        public string Expression { get; private set; }
        public List<Property> Properties = [];
        public List<PrincipalProperty> PrincipalProperties = [];

        internal Filter(string expression, EntityType entityType)
        {
            Expression = expression;
        }
    }

    internal abstract class Order(string propertyName)
    {
        public string PropertyName { get; private set; } = propertyName;
    }

    internal class AscendingOrder(string propertyName) : Order(propertyName)
    {
    }

    internal class DescendingOrder(string propertyName) : Order(propertyName)
    {
    }

    internal abstract partial class QueryBase
    {
        public EntityType EntityType { get; private set; }

        public string[] Select { get; private set; }

        public Filter? Filter { get; private set; } = null;

        public Order[]? Orderby { get; private set; } = null;

        public long? Top { get; private set; } = null;

        public long? Skip { get; private set; } = null;

        public ExpandQuery[]? Expands { get; protected set; } = null;

    }
}
