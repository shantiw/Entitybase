using Shantiw.Data.Schema;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Meta
{
    internal class ManyToManyAssociation
    {
        public string Name { get; private set; }

        public Association FirstAssociation { get; private set; } // principal to dependentEnd

        public Association SecondAssociation { get; private set; } // principal to dependentEnd

        public ManyToManyAssociation(XElement xAssociation, IReadOnlyDictionary<string, EntityType> entityTypes)
        {
            Name = xAssociation.GetAttributeValue(SchemaVocab.Name);

            EntityDataModel model = entityTypes.Values.First().EntityDataModel;
            XElement xEntityType = xAssociation.Elements(SchemaVocab.EntityType).Single();
            EntityType entityType = new(model, xEntityType);

            Debug.Assert(xAssociation.Elements(SchemaVocab.ReferentialConstraint).Count() == 2);

            List<Association> associations = [];
            foreach (XElement xReferentialConstraint in xAssociation.Elements(SchemaVocab.ReferentialConstraint))
            {
                string name = xReferentialConstraint.GetAttributeValue(SchemaVocab.Name);
                AssociationEnd principalEnd = CreatePrincipalEnd(xReferentialConstraint, xAssociation, entityTypes);
                AssociationEnd dependentEnd = CreateDependentEnd(xReferentialConstraint, entityType);
                associations.Add(new Association(model, name, principalEnd, dependentEnd));
            }

            FirstAssociation = associations.First();
            SecondAssociation = associations.Last();
        }

        private static AssociationEnd CreatePrincipalEnd(XElement xReferentialConstraint, XElement xAssociation, IReadOnlyDictionary<string, EntityType> entityTypes)
        {
            XElement? xPrincipal = xReferentialConstraint.Element(SchemaVocab.Principal) ?? throw new ArgumentException(xReferentialConstraint.ToString());

            string role = xPrincipal.GetAttributeValue(SchemaVocab.Role);
            XElement xEnd = xAssociation.Elements(SchemaVocab.End).Single(e => e.GetAttributeValue(SchemaVocab.Role) == role);

            string type = xEnd.GetAttributeValue(SchemaVocab.Type);
            EntityType entityType = entityTypes[type];

            List<Property> properties = [];
            string multiplicity = Multiplicity.One;
            List<XElement> xPropertyRefs = [.. xPrincipal.Elements(SchemaVocab.PropertyRef)];
            if (xPropertyRefs.Count == 1)
            {
                Property property = entityType.Properties[xPropertyRefs.Single().GetAttributeValue(SchemaVocab.Name)];
                multiplicity = property.Nullable ? Multiplicity.ZeroOne : Multiplicity.One;
                properties.Add(property);
            }
            else
            {
                foreach (XElement xPropertyRef in xPropertyRefs.OrderBy(p => p.GetAttributeValue(SchemaVocab.Order)))
                {
                    Property property = entityType.Properties[xPropertyRef.GetAttributeValue(SchemaVocab.Name)];
                    multiplicity = property.Nullable ? Multiplicity.ZeroOne : multiplicity;
                    properties.Add(property);
                }
            }

            return new(role, multiplicity, entityType, [.. properties]);
        }

        private static AssociationEnd CreateDependentEnd(XElement xReferentialConstraint, EntityType entityType)
        {
            XElement? xDependent = xReferentialConstraint.Element(SchemaVocab.Dependent) ?? throw new ArgumentException(xReferentialConstraint.ToString());

            string role = xDependent.GetAttributeValue(SchemaVocab.Role);

            List<Property> properties = [];
            List<XElement> xPropertyRefs = [.. xDependent.Elements(SchemaVocab.PropertyRef)];
            if (xPropertyRefs.Count == 1)
            {
                Property property = entityType.Properties[xPropertyRefs.Single().GetAttributeValue(SchemaVocab.Name)];
                properties.Add(property);
            }
            else
            {
                foreach (XElement xPropertyRef in xPropertyRefs.OrderBy(p => p.GetAttributeValue(SchemaVocab.Order)))
                {
                    Property property = entityType.Properties[xPropertyRef.GetAttributeValue(SchemaVocab.Name)];
                    properties.Add(property);
                }
            }

            return new(role, Multiplicity.Many, entityType, [.. properties]);
        }

    }
}
