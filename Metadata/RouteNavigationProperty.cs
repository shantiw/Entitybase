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

        private readonly string _fromMultiplicity;
        public override string FromMultiplicity { get { return _fromMultiplicity; } }

        private readonly string _toMultiplicity;
        public override string ToMultiplicity { get { return _toMultiplicity; } }

        internal RouteNavigationProperty(EntityType entityType, XElement xNavigationProperty)
            : base(entityType, xNavigationProperty) // <NavigationProperty Name="..." Route="..." />
        {
            string route = xNavigationProperty.GetAttributeValue(nameof(RouteNavigationProperty.Route));
            List<VectorialAssociation> routeList = [];
            string toMultiplicity = Multiplicity.One;
            EntityType current = EntityType;
            foreach (string navigationPropertyName in route.Split('.'))
            {
                NavigationProperty navigationProperty = current.NavigationProperties[navigationPropertyName];

                if (toMultiplicity == Multiplicity.One)
                {
                    toMultiplicity = navigationProperty.ToMultiplicity;
                }
                else if (toMultiplicity == Multiplicity.ZeroOne)
                {
                    if (navigationProperty.ToMultiplicity == Multiplicity.Many)
                    {
                        toMultiplicity = Multiplicity.Many;
                    }
                }

                VectorialAssociation[] oRoute = navigationProperty.Route;
                routeList.AddRange(oRoute);
                current = oRoute[^1].ToEnd.EntityType;
            }

            _route = [.. routeList];
            _fromMultiplicity = _route[0].FromEnd.Multiplicity;
            _toMultiplicity = toMultiplicity;
        }

    }
}
