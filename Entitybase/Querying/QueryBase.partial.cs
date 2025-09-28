using Shantiw.Data.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Querying
{
    internal partial class QueryBase
    {
        protected QueryBase(XElement xQueryOrExpand, EntityType entityType)
        {
            EntityType = entityType;

            //
            XElement? xSelect = xQueryOrExpand.Element(QueryVocab.Select);
            Select = GetSelect(xSelect?.Value);

            //
            XElement? xFilter = xQueryOrExpand.Element(nameof(Filter));
            if (xFilter != null)
            {
                Filter = new Filter(xFilter.Value, EntityType);
            }

            //
            XElement? xOrderby = xQueryOrExpand.Element(QueryVocab.Orderby);
            if (xOrderby != null)
            {
                Orderby = GetOrderby(xOrderby.Value);
            }

            //
            XElement? xTop = xQueryOrExpand.Element(nameof(Top));
            if (xTop != null)
            {
                if (long.TryParse(xTop.Value, out long top))
                {
                    Top = top;
                }
                else
                {
                    throw new ArgumentException($"The value of Top must be a valid integer.");
                }
            }

            //
            XElement? xSkip = xQueryOrExpand.Element(nameof(Skip));
            if (xSkip != null)
            {
                if (long.TryParse(xSkip.Value, out long skip))
                {
                    Skip = skip;
                }
                else
                {
                    throw new ArgumentException($"The value of Skip must be a valid integer.");
                }
            }

            //
            List<ExpandQuery> expands = [];
            foreach (XElement xExpand in xQueryOrExpand.Elements(QueryVocab.Expand))
            {
                NavigationProperty navigationProperty = GetNavigationProperty(xExpand, entityType);
                expands.Add(new ExpandQuery(xExpand, navigationProperty));
            }
            Expands = [.. expands];
        }

        private string[] GetSelect(string? select)
        {
            if (string.IsNullOrWhiteSpace(select))
                return [.. EntityType.ScalarProperties.Keys];

            return [.. select.Split(',', StringSplitOptions.TrimEntries)];
        }

        private static Order[] GetOrderby(string orderby)
        {
            List<Order> orders = [];
            string[] orderClauses = [.. orderby.Split(',', StringSplitOptions.TrimEntries)];
            foreach (string orderClause in orderClauses)
            {
                string[] parts = [.. orderClause.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];
                if (parts.Length == 1)
                {
                    orders.Add(new AscendingOrder(parts[0]));
                }
                else if (parts.Length == 2)
                {
                    if (parts[1].Equals("asc", StringComparison.OrdinalIgnoreCase))
                    {
                        orders.Add(new AscendingOrder(parts[0]));
                    }
                    else if (parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase))
                    {
                        orders.Add(new DescendingOrder(parts[0]));
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
            return [.. orders];
        }

        private static NavigationProperty GetNavigationProperty(XElement xExpand, EntityType entityType)
        {
            XElement xNavigationProperty = xExpand.Element(nameof(NavigationProperty))
                ?? throw new ArgumentException($"Expand must have a {nameof(NavigationProperty)} element.");
            return entityType.NavigationProperties[xNavigationProperty.Value];
        }

    }
}
