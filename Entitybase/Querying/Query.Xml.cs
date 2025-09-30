using Shantiw.Data.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Querying
{
    public abstract partial class QueryBase
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

        private static NavigationProperty GetNavigationProperty(XElement xExpand, EntityType entityType)
        {
            XElement xNavigationProperty = xExpand.Element(nameof(NavigationProperty))
                ?? throw new ArgumentException($"Expand must have a {nameof(NavigationProperty)} element.");
            return entityType.NavigationProperties[xNavigationProperty.Value];
        }

    }

    public partial class Query : QueryBase
    {
        public Query(XElement xQuery, EntityDataModel model) : base(xQuery, GetEntityType(xQuery, model))
        {
        }

        private static EntityType GetEntityType(XElement xQuery, EntityDataModel model)
        {
            XElement? xEntitySet = xQuery.Element(QueryVocab.EntitySet);
            if (xEntitySet == null)
            {
                XElement xEntityType = xQuery.Element(nameof(EntityType))
                    ?? throw new ArgumentException($"Query must have an {QueryVocab.EntitySet} or an {nameof(EntityType)} element.");
                return model.EntityTypes[xEntityType.Value];
            }
            return model.GetEntityTypeByEntitySetName(xEntitySet.Value);
        }

    }

    public partial class ExpandQuery : QueryBase
    {
        public NavigationProperty NavigationPropertyOfParent { get; private set; }

        internal ExpandQuery(XElement xExpand, NavigationProperty navigationPropertyOfParent)
            : base(xExpand, navigationPropertyOfParent.Path[^1].ToEnd.EntityType)
        {
            NavigationPropertyOfParent = navigationPropertyOfParent;
        }

    }

}
