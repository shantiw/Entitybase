using Shantiw.Data.DataAnnotations;
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
    internal class Query(XElement xQuery, EntityDataModel model) : QueryBase(xQuery, GetEntityType(xQuery, model))
    {
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

    internal class ExpandQuery(XElement xExpand, NavigationProperty navigationPropertyOfParent)
        : QueryBase(xExpand, navigationPropertyOfParent.Path[^1].ToEnd.EntityType)
    {
        public NavigationProperty NavigationPropertyOfParent { get; private set; } = navigationPropertyOfParent;
    }

}
