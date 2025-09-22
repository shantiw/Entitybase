using Shantiw.Data.DataAnnotations;
using Shantiw.Data.Schema;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Meta
{
    public class PrincipalProperty
    {
        public EntityType EntityType { get; private set; }

        public string Name { get; private set; }

        public VectorialAssociation[] Route { get; private set; }

        public Property PropertyRef { get; private set; }

        private string? _displayName = null;
        public string DisplayName
        {
            get
            {
                if (_displayName == null)
                {
                    string? displayName = AttributeUtil.GetDisplayName(ComponentModelAttributes);
                    _displayName = displayName ?? Name;
                }
                return _displayName;
            }
        }

        public IReadOnlyDictionary<string, Attribute> ComponentModelAttributes { get; private set; } // DisplayAttribute

        internal PrincipalProperty(EntityType entityType, XElement xPrincipalProperty)
        {
            EntityType = entityType;
            Name = xPrincipalProperty.GetAttributeValue(SchemaVocab.Name);

            //
            string route = xPrincipalProperty.GetAttributeValue(nameof(RouteNavigationProperty.Route));
            string nameOfPropertyRef = xPrincipalProperty.GetAttributeValue(MetaVocab.NameOfPropertyRef);

            List<VectorialAssociation> routeList = [];
            EntityType current = EntityType;
            foreach (string navigationPropertyName in route.Split('.'))
            {
                NavigationProperty navigationProperty = current.NavigationProperties[navigationPropertyName];
                VectorialAssociation[] oRoute = navigationProperty.Route;

                if (oRoute.Length != 1) throw new ArgumentException(navigationProperty.Name);
                if (oRoute[0].ToEnd.Multiplicity == Multiplicity.Many) throw new ArgumentException(oRoute[0].Name);

                routeList.Add(oRoute[0]);
                current = oRoute[0].ToEnd.EntityType;
            }

            Route = [.. routeList];
            PropertyRef = current.Properties[nameOfPropertyRef];

            //
            ComponentModelAttributes = AttributeUtil.CreateComponentModelAttributes(xPrincipalProperty);
        }

    }
}
