using Shantiw.Data.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Meta
{
    public abstract class PropertyBase
    {
        public EntityType EntityType { get; private set; }

        public string Name { get; private set; }

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

        public IReadOnlyDictionary<string, Attribute> ComponentModelAttributes { get; private set; }

        internal PropertyBase(EntityType entityType, XElement xProperty)
        {
            EntityType = entityType;
            Name = xProperty.GetAttributeValue(SchemaVocab.Name);

            ComponentModelAttributes = AttributeUtil.CreateComponentModelAttributes(xProperty);
        }

    }
}
