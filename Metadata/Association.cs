using Shantiw.Data.Schema;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Meta
{
    internal class Association
    {
        public string Name { get; private set; }

        public AssociationEnd PrincipalEnd { get; private set; }

        public AssociationEnd DependentEnd { get; private set; }

        public Association(XElement xAssociation, IReadOnlyDictionary<string, EntityType> entityTypes)
        {
            Name = xAssociation.GetAttributeValue(SchemaVocab.Name);

            XElement? xReferentialConstraint = xAssociation.Element(SchemaVocab.ReferentialConstraint)
                ?? throw new ArgumentException(xAssociation.ToString());

            XElement? xPrincipal = xReferentialConstraint.Element(SchemaVocab.Principal) ?? throw new ArgumentException(xReferentialConstraint.ToString());
            PrincipalEnd = new(xPrincipal, xAssociation, entityTypes);

            XElement? xDependent = xReferentialConstraint.Element(SchemaVocab.Dependent) ?? throw new ArgumentException(xReferentialConstraint.ToString());
            DependentEnd = new(xDependent, xAssociation, entityTypes);

            Debug.Assert(PrincipalEnd.Properties.Length == DependentEnd.Properties.Length);
        }

        public Association(EntityDataModel model, string name, AssociationEnd principalEnd, AssociationEnd dependentEnd) // ManyToMany
        {
            Name = name;
            PrincipalEnd = principalEnd;
            DependentEnd = dependentEnd;
        }

    }
}
