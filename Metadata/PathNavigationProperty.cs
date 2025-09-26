using Shantiw.Data.DataAnnotations;
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
        private readonly VectorialAssociation[] _path;
        public override VectorialAssociation[] Path { get { return _path; } }

        private readonly string _fromMultiplicity;
        public override string FromMultiplicity { get { return _fromMultiplicity; } }

        private readonly string _toMultiplicity;
        public override string ToMultiplicity { get { return _toMultiplicity; } }

        internal PathNavigationProperty(EntityType entityType, XElement xNavigationProperty)
            : base(entityType, xNavigationProperty) // <NavigationProperty Name="..." Path="..." />
        {
            string path = xNavigationProperty.GetAttributeValue(nameof(PathNavigationProperty.Path));
            List<VectorialAssociation> pathList = [];
            string toMultiplicity = Multiplicity.One;
            EntityType current = EntityType;
            foreach (string navigationPropertyName in path.Split('.'))
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

                VectorialAssociation[] oPath = navigationProperty.Path;
                pathList.AddRange(oPath);
                current = oPath[^1].ToEnd.EntityType;
            }

            _path = [.. pathList];
            _fromMultiplicity = _path[0].FromEnd.Multiplicity;
            _toMultiplicity = toMultiplicity;
        }

    }
}
