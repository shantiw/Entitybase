using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Meta
{
    public class ComputedProperty : PropertyBase
    {
        public ExpressionObject ExpressionObject { get; private set; }

        internal ComputedProperty(EntityType entityType, XElement xComputedProperty) : base(entityType, xComputedProperty)
        {
            string expression = xComputedProperty.GetAttributeValue(nameof(ExpressionObject.Expression));
            ExpressionObject = new ExpressionObject(expression, entityType);
        }

    }
}
