using Shantiw.Data.DataAnnotations;
using Shantiw.Data.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Meta
{
    internal class RouteNavigationProperty : NavigationProperty
    {
        private readonly VectorialAssociation[] _route;
        public override VectorialAssociation[] Route { get { return _route; } }

        internal RouteNavigationProperty(EntityType entityType, XElement xNavigationProperty)
            : base(entityType, xNavigationProperty) // <NavigationProperty Name="..." Route="..." />
        {
            string route = xNavigationProperty.GetAttributeValue(nameof(RouteNavigationProperty.Route));
            List<VectorialAssociation> routeList = [];
            EntityType current = EntityType;
            foreach (string navigationPropertyName in route.Split('.'))
            {
                VectorialAssociation[] oRoute = current.NavigationProperties[navigationPropertyName].Route;
                routeList.AddRange(oRoute);
                current = oRoute[^1].ToEnd.EntityType;
            }

            _route = [.. routeList];
        }

    }
}
