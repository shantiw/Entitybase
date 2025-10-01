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
    public abstract class NavigationProperty : PropertyBase
    {
        public abstract VectorialAssociation[] Vector { get; }

        public abstract string FromMultiplicity { get; }

        public abstract string ToMultiplicity { get; }

        internal NavigationProperty(EntityType entityType, XElement xNavigationProperty) : base(entityType, xNavigationProperty)
        {
        }

    }
}
