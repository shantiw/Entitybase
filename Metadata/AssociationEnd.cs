using Shantiw.Data.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Meta
{
    public class AssociationEnd
    {
        public string Role { get; private set; }

        public string Multiplicity { get; private set; }

        public EntityType EntityType { get; private set; }

        public Property[] Properties { get; private set; }

        internal AssociationEnd(XElement xPrincipalOrDependent, XElement xAssociation, IReadOnlyDictionary<string, EntityType> entityTypes)
        {
            Role = xPrincipalOrDependent.GetAttributeValue(SchemaVocab.Role);

            XElement xEnd = xAssociation.Elements(SchemaVocab.End).Single(e => e.GetAttributeValue(SchemaVocab.Role) == Role);

            string type = xEnd.GetAttributeValue(SchemaVocab.Type);
            EntityType = entityTypes[type];

            Multiplicity = xEnd.GetAttributeValue(nameof(Multiplicity));

            // 
            List<Property> properties = [];
            List<XElement> xPropertyRefs = [.. xPrincipalOrDependent.Elements(SchemaVocab.PropertyRef)];
            if (xPropertyRefs.Count == 1)
            {
                properties.Add(EntityType.Properties[xPropertyRefs.Single().GetAttributeValue(SchemaVocab.Name)]);
            }
            else
            {
                foreach (XElement xPropertyRef in xPropertyRefs.OrderBy(p => p.GetAttributeValue(SchemaVocab.Order)))
                {
                    properties.Add(EntityType.Properties[xPropertyRef.GetAttributeValue(SchemaVocab.Name)]);
                }
            }

            Properties = [.. properties];
        }

        internal AssociationEnd(string role, string multiplicity, EntityType entityType, Property[] properties) // ManyToMany
        {
            Role = role;
            Multiplicity = multiplicity;
            EntityType = entityType;
            Properties = properties;
        }

    }
}
