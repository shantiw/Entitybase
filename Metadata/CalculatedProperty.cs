using Shantiw.Data.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Meta
{
    public class CalculatedProperty
    {
        public EntityType EntityType { get; private set; }

        public string Name { get; private set; }

        public Type Type { get; private set; }

        public string Expression { get; private set; } // DataColumn.Expression

        public bool Nullable { get; private set; }

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

        internal CalculatedProperty(EntityType entityType, XElement xProperty)
        {
            EntityType = entityType;

            Name = xProperty.GetAttributeValue(nameof(Name));
            string typeName = xProperty.GetAttributeValue(nameof(Type));
            Type? type = Type.GetType(MetaVocab.TypePrefix + typeName, true);
            Type = type ?? throw new ArgumentNullException($"Could not find class type {typeName}.");
            Nullable = true;
            Expression = xProperty.GetAttributeValue(nameof(Expression));
            //
            if (xProperty.Elements(SchemaVocab.Annotation).Any())
            {
                XElement dProperty = new(SchemaVocab.Property, new XAttribute(nameof(Name), Name));
                dProperty.Add(xProperty.Elements(SchemaVocab.Annotation));
            }

            //
            ComponentModelAttributes = AttributeUtil.CreatePropertyComponentModelAttributes(xProperty);
        }

    }
}
