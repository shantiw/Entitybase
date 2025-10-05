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
        public string Expression { get; private set; }

        public PreprocessedClause PreprocessedClause { get; private set; }

        internal ComputedProperty(EntityType entityType, XElement xComputedProperty) : base(entityType, xComputedProperty)
        {
            Expression = xComputedProperty.GetAttributeValue(nameof(Expression));
            PreprocessedClause = new PreprocessedClause(Expression, entityType);
        }

    }
}
