using Shantiw.Data.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Meta
{
    internal class PathNavigationProperty : NavigationProperty
    {
        public string Path { get; private set; }

        private readonly VectAssociation[] _vector;
        public override VectAssociation[] Vector { get { return _vector; } }

        private readonly string _fromMultiplicity;
        public override string FromMultiplicity { get { return _fromMultiplicity; } }

        private readonly string _toMultiplicity;
        public override string ToMultiplicity { get { return _toMultiplicity; } }

        internal PathNavigationProperty(EntityType entityType, XElement xNavigationProperty)
            : base(entityType, xNavigationProperty) // <NavigationProperty Name="..." Path="..." />
        {
            string path = xNavigationProperty.GetAttributeValue(nameof(PathNavigationProperty.Path));
            string[] pathArray = path.Split(['.', ',', '\\', '/'], StringSplitOptions.TrimEntries);
            Path = string.Join('/', pathArray);
            List<VectAssociation> vector = [];
            string toMultiplicity = Multiplicity.One;
            EntityType current = EntityType;
            foreach (string navigationPropertyName in pathArray)
            {
                NavigationProperty navigationProperty = current.NavigationProperties[navigationPropertyName];

                if (toMultiplicity == Multiplicity.One)
                {
                    toMultiplicity = navigationProperty.ToMultiplicity;
                }
                else if (toMultiplicity == Multiplicity.ZeroOne)
                {
                    if (navigationProperty.ToMultiplicity == Multiplicity.Many)
                    {
                        toMultiplicity = Multiplicity.Many;
                    }
                }

                VectAssociation[] associationArray = navigationProperty.Vector;
                vector.AddRange(associationArray);
                current = associationArray[^1].ToEnd.EntityType;
            }

            _vector = [.. vector];
            _fromMultiplicity = _vector[0].FromEnd.Multiplicity;
            _toMultiplicity = toMultiplicity;
        }

    }
}
