using Shantiw.Data.DataAnnotations;
using Shantiw.Data.Schema;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Meta
{
    public abstract class NavigationProperty
    {
        public EntityType EntityType { get; private set; }

        public string Name { get; private set; }

        public abstract VectorialAssociation[] Route { get; }

        public abstract string FromMultiplicity { get; }

        public abstract string ToMultiplicity { get; }

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

        internal NavigationProperty(EntityType entityType, XElement xNavigationProperty)
        {
            EntityType = entityType;
            Name = xNavigationProperty.GetAttributeValue(SchemaVocab.Name);

            ComponentModelAttributes = AttributeUtil.CreateComponentModelAttributes(xNavigationProperty);
        }

    }
}
