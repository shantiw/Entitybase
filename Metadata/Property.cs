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
    public class Property : PropertyBase
    {
        public Type Type { get; private set; }

        public bool Nullable { get; private set; }

        public string ColumnName { get; private set; }

        public IReadOnlyDictionary<string, Attribute> DataAnnotations { get; private set; } // ConcurrencyCheckAttribute, EditableAttribute, TimestampAttribute

        public IReadOnlyDictionary<string, ValidationAttribute> ValidationAttributes { get; private set; }

        internal Property(EntityType entityType, XElement xProperty) : base(entityType, xProperty)
        {
            string typeName = xProperty.GetAttributeValue(SchemaVocab.Type);
            Type? type = Type.GetType(MetaVocab.TypePrefix + typeName, true);
            Type = type ?? throw new ArgumentNullException($"Could not find class type {typeName}.");
            Nullable = bool.Parse(xProperty.GetAttributeValue(SchemaVocab.Nullable));
            ColumnName = xProperty.GetAttributeValue(SchemaVocab.ColumnName);

            //
            DataAnnotations = AttributeUtil.CreatePropertyDataAnnotations(xProperty);
            ValidationAttributes = AttributeUtil.CreatePropertyValidations(xProperty);
        }

    }
}
