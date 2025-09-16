using Shantiw.Data.DataAnnotations;
using Shantiw.Data.Schema;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Meta
{
    public partial class Property
    {
        public EntityType EntityType { get; private set; }

        public string Name { get; private set; }

        public Type Type { get; private set; }

        public bool Nullable { get; private set; }

        public string ColumnName { get; private set; }

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

        public IReadOnlyDictionary<string, Attribute> ComponentModelAttributes { get; private set; } // ConcurrencyCheckAttribute, DisplayAttribute, EditableAttribute, TimestampAttribute

        public IReadOnlyDictionary<string, ValidationAttribute> ValidationAttributes { get; private set; }

        internal Property(EntityType entityType, XElement xProperty)
        {
            EntityType = entityType;

            Name = xProperty.GetAttributeValue(SchemaVocab.Name);
            string typeName = xProperty.GetAttributeValue(SchemaVocab.Type);
            Type? type = Type.GetType(MetaVocab.TypePrefix + typeName, true);
            Type = type ?? throw new ArgumentNullException($"Could not find class type {typeName}.");
            Nullable = bool.Parse(xProperty.GetAttributeValue(SchemaVocab.Nullable));
            ColumnName = xProperty.GetAttributeValue(SchemaVocab.ColumnName);

            //
            if (xProperty.Elements(SchemaVocab.Annotation).Any())
            {
                XElement dProperty = new(SchemaVocab.Property, new XAttribute(SchemaVocab.Name, Name));
                dProperty.Add(xProperty.Elements(SchemaVocab.Annotation));
            }

            //
            ComponentModelAttributes = AttributeUtil.CreatePropertyComponentModelAttributes(xProperty);
            ValidationAttributes = AttributeUtil.CreateValidationAttributes(xProperty);
        }

    }
}
