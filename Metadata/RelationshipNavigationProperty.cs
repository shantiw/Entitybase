using Shantiw.Data.Schema;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Meta
{
    internal class RelationshipNavigationProperty : NavigationProperty
    {
        private readonly VectAssociation[] _vector;
        public override VectAssociation[] Vector { get { return _vector; } }

        private readonly string _fromMultiplicity;
        public override string FromMultiplicity { get { return _fromMultiplicity; } }

        private readonly string _toMultiplicity;
        public override string ToMultiplicity { get { return _toMultiplicity; } }

        internal RelationshipNavigationProperty(EntityType entityType, XElement xNavigationProperty)
            : base(entityType, xNavigationProperty) // <NavigationProperty Name="..." Relationship="..." FromRole="..." ToRole="..." />
        {
            string relationship = xNavigationProperty.GetAttributeValue(SchemaVocab.Relationship);
            string fromRole = xNavigationProperty.GetAttributeValue(SchemaVocab.FromRole);
            string toRole = xNavigationProperty.GetAttributeValue(SchemaVocab.ToRole);
            if (entityType.EntityDataModel.Associations.TryGetValue(relationship, out Association? association))
            {
                VectAssociation vectAssociation;
                if (association.PrincipalEnd.Role == fromRole)
                {
                    Debug.Assert(association.DependentEnd.Role == toRole);

                    vectAssociation = new(relationship, association.PrincipalEnd, association.DependentEnd);
                }
                else
                {
                    Debug.Assert(association.PrincipalEnd.Role == toRole);
                    Debug.Assert(association.DependentEnd.Role == fromRole);

                    vectAssociation = new(relationship, association.DependentEnd, association.PrincipalEnd);
                }

                _vector = [vectAssociation];
                _fromMultiplicity = vectAssociation.FromEnd.Multiplicity;
                _toMultiplicity = vectAssociation.ToEnd.Multiplicity;
            }
            else // ManyToMany
            {
                VectAssociation vectAssociation1, vectAssociation2;
                ManyToManyAssociation manyToMany = entityType.EntityDataModel.ManyToManyAssociations[relationship];
                if (manyToMany.FirstAssociation.PrincipalEnd.Role == fromRole)
                {
                    Debug.Assert(manyToMany.SecondAssociation.PrincipalEnd.Role == toRole);

                    vectAssociation1 = new(manyToMany.FirstAssociation.Name, manyToMany.FirstAssociation.PrincipalEnd, manyToMany.FirstAssociation.DependentEnd);
                    vectAssociation2 = new(manyToMany.SecondAssociation.Name, manyToMany.SecondAssociation.DependentEnd, manyToMany.SecondAssociation.PrincipalEnd);
                }
                else
                {
                    Debug.Assert(manyToMany.FirstAssociation.PrincipalEnd.Role == toRole);
                    Debug.Assert(manyToMany.SecondAssociation.PrincipalEnd.Role == fromRole);

                    vectAssociation1 = new(manyToMany.SecondAssociation.Name, manyToMany.SecondAssociation.PrincipalEnd, manyToMany.SecondAssociation.DependentEnd);
                    vectAssociation2 = new(manyToMany.FirstAssociation.Name, manyToMany.FirstAssociation.DependentEnd, manyToMany.FirstAssociation.PrincipalEnd);
                }

                _vector = [vectAssociation1, vectAssociation2];
                _fromMultiplicity = Multiplicity.Many;
                _toMultiplicity = Multiplicity.Many;
            }
        }

    }
}
