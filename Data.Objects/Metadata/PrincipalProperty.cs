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
    public class PrincipalProperty : PropertyBase
    {
        public NavigationProperty NavigationProperty { get; private set; }

        public Property PropertyRef { get; private set; }

        internal PrincipalProperty(EntityType entityType, XElement xPrincipalProperty) : base(entityType, xPrincipalProperty)
        {
            string nameOfNavigationPropertyRef = xPrincipalProperty.GetAttributeValue(MetaVocab.NameOfNavigationPropertyRef);
            string nameOfPropertyRef = xPrincipalProperty.GetAttributeValue(MetaVocab.NameOfPropertyRef);

            NavigationProperty = EntityType.NavigationProperties[nameOfNavigationPropertyRef];
            if (NavigationProperty.ToMultiplicity == Multiplicity.Many) throw new ArgumentException("The toMultiplicity of the navigationProperty must not be Many.");

            PropertyRef = NavigationProperty.Vector[^1].ToEnd.EntityType.Properties[nameOfPropertyRef];
        }

    }
}
