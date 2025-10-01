using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Meta
{
    public class CalculatedProperty : PropertyBase
    {
        public Type Type { get; private set; }

        public string Expression { get; private set; } // DataColumn.Expression

        public bool Nullable { get; private set; }

        internal CalculatedProperty(EntityType entityType, XElement xCalculatedProperty) : base(entityType, xCalculatedProperty)
        {
            string typeName = xCalculatedProperty.GetAttributeValue(nameof(Type));
            Type? type = Type.GetType(MetaVocab.TypePrefix + typeName, true);
            Type = type ?? throw new ArgumentNullException($"Could not find class type {typeName}.");
            Nullable = true;
            Expression = xCalculatedProperty.GetAttributeValue(nameof(Expression));
        }

    }
}
