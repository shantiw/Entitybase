using Shantiw.Data.DataAnnotations;
using Shantiw.Data.Meta;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shantiw.Data.Querying
{
    public class Select
    {
        public string[] Properties { get; private set; }

        internal Select(string? select, EntityType entityType)
        {
            if (string.IsNullOrWhiteSpace(select))
            {
                if (entityType.StorageAttributes.TryGetValue(nameof(SelectAttribute), out StorageAttribute? attr))
                {
                    if (attr is SelectAttribute selectAttribute)
                    {
                        Properties = [.. selectAttribute.Default];
                        return;
                    }

                    Debug.Assert(attr is SelectAttribute);
                }

                //
                List<string> properties = [];
                foreach (KeyValuePair<string, PropertyBase> pair in entityType.ScalarProperties)
                {
                    if (pair.Value is Property prop)
                    {
                        if (prop.Type == typeof(byte[])) continue;
                        properties.Add(pair.Key);
                    }
                }
                Properties = [.. properties];
            }
            else
                Properties = [.. select.Split(',', StringSplitOptions.TrimEntries)];
        }

    }

    public class Filter
    {       
        public ExpressionObject ExpressionObject { get; private set; }

        internal Filter(string expression, EntityType entityType)
        {
            ExpressionObject = new ExpressionObject(expression, entityType);
        }

    }

    public abstract class SortOrder(string property)
    {
        public string Property => property;
    }

    public class AscendingOrder(string propertyName) : SortOrder(propertyName)
    {
    }

    public class DescendingOrder(string propertyName) : SortOrder(propertyName)
    {
    }

    public class OrderBy
    {
        public SortOrder[] SortOrders { get; private set; }

        public OrderBy(string orderby)
        {
            List<SortOrder> sortOrders = [];
            string[] orderClauses = [.. orderby.Split(',', StringSplitOptions.TrimEntries)];
            foreach (string orderClause in orderClauses)
            {
                string[] parts = [.. orderClause.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];
                if (parts.Length == 1)
                {
                    sortOrders.Add(new AscendingOrder(parts[0]));
                }
                else if (parts.Length == 2)
                {
                    if (parts[1].Equals("asc", StringComparison.OrdinalIgnoreCase))
                    {
                        sortOrders.Add(new AscendingOrder(parts[0]));
                    }
                    else if (parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase))
                    {
                        sortOrders.Add(new DescendingOrder(parts[0]));
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid order direction '{parts[1]}'. Use 'asc' or 'desc'.");
                    }
                }
                else
                {
                    throw new ArgumentException($"Invalid order clause '{orderClause}'.");
                }
            }
            SortOrders = [.. sortOrders];
        }

    }

    public abstract partial class QueryBase
    {
        public EntityType EntityType { get; private set; }

        public Select Select { get; private set; }

        public Filter? Filter { get; private set; } = null;

        public OrderBy? OrderBy { get; private set; } = null;

        public long? Top { get; private set; } = null;

        public long? Skip { get; private set; } = null;

        public ExpandQuery[] Expands { get; protected set; } = [];

    }
}
