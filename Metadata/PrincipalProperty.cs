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

        public NavigationProperty NavigationProperty { get; private set; }

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
            string nameOfNavigationPropertyRef = xPrincipalProperty.GetAttributeValue(MetaVocab.NameOfNavigationPropertyRef);
            string nameOfPropertyRef = xPrincipalProperty.GetAttributeValue(MetaVocab.NameOfPropertyRef);

            NavigationProperty = EntityType.NavigationProperties[nameOfNavigationPropertyRef];
            if (NavigationProperty.ToMultiplicity == Multiplicity.Many) throw new ArgumentException("The toMultiplicity of the navigationProperty must not be Many.");

            PropertyRef = NavigationProperty.Path[^1].ToEnd.EntityType.Properties[nameOfPropertyRef];

            //
            ComponentModelAttributes = AttributeUtil.CreateComponentModelAttributes(xPrincipalProperty);
        }

    }
}
