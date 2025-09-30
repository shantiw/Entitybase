using Shantiw.Data.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Shantiw.Data.Querying
{
    public abstract partial class QueryBase
    {
        protected QueryBase(JsonElement jQueryOrExpand, EntityType entityType)
        {
            EntityType = entityType;

            //
            string? select = null;
            if (jQueryOrExpand.TryGetProperty(QueryVocab.Select, out JsonElement jSelect))
            {
                select = jSelect.ToString();
            }
            Select = GetSelect(select);

            //
            if (jQueryOrExpand.TryGetProperty(nameof(Filter), out JsonElement jFilter))
            {
                Filter = new Filter(jFilter.ToString(), EntityType);
            }

            //
            if (jQueryOrExpand.TryGetProperty(QueryVocab.Orderby, out JsonElement jOrderby))
            {
                Orderby = GetOrderby(jOrderby.ToString());
            }

            //
            if (jQueryOrExpand.TryGetProperty(nameof(Top), out JsonElement jTop))
            {
                if (jTop.TryGetInt64(out long top))
                {
                    Top = top;
                }
                else
                {
                    throw new ArgumentException($"The value of Top must be a valid integer.");
                }
            }

            //
            if (jQueryOrExpand.TryGetProperty(nameof(Skip), out JsonElement jSkip))
            {
                if (jSkip.TryGetInt64(out long skip))
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
            if (jQueryOrExpand.TryGetProperty(QueryVocab.Expand, out JsonElement jExpand))
            {
                if (jExpand.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement jItem in jExpand.EnumerateArray())
                    {
                        NavigationProperty jNavigationProperty = GetNavigationProperty(jItem, entityType);
                        expands.Add(new ExpandQuery(jItem, jNavigationProperty));
                    }
                }
                else
                {
                    NavigationProperty jNavigationProperty = GetNavigationProperty(jExpand, entityType);
                    expands.Add(new ExpandQuery(jExpand, jNavigationProperty));
                }
            }
            Expands = [.. expands];
        }

        private static NavigationProperty GetNavigationProperty(JsonElement jExpand, EntityType entityType)
        {
            if (jExpand.TryGetProperty(nameof(NavigationProperty), out JsonElement jNavigationProperty))
            {
                return entityType.NavigationProperties[jNavigationProperty.ToString()];
            }
            throw new ArgumentException($"Expand must have a {nameof(NavigationProperty)} element.");
        }

    }

    public partial class Query : QueryBase
    {
        public Query(JsonDocument jsonDocument, EntityDataModel model) : base(jsonDocument.RootElement, GetEntityType(jsonDocument, model))
        {
        }

        private static EntityType GetEntityType(JsonDocument jsonDocument, EntityDataModel model)
        {
            JsonElement jsonElement = jsonDocument.RootElement;
            if (jsonElement.TryGetProperty(QueryVocab.EntitySet, out JsonElement jEntitySet))
            {
                return model.GetEntityTypeByEntitySetName(jEntitySet.ToString());
            }

            JsonElement jEntity = jsonElement.GetProperty(nameof(EntityType));
            return model.EntityTypes[jEntity.ToString()];
        }

    }

    public partial class ExpandQuery : QueryBase
    {
        internal ExpandQuery(JsonElement jExpand, NavigationProperty navigationPropertyOfParent)
            : base(jExpand, navigationPropertyOfParent.Path[^1].ToEnd.EntityType)
        {
            NavigationPropertyOfParent = navigationPropertyOfParent;
        }

    }

}
