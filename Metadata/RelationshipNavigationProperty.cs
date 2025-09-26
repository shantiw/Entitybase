using Shantiw.Data.DataAnnotations;
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
        private readonly VectorialAssociation[] _path;
        public override VectorialAssociation[] Path { get { return _path; } }

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
                VectorialAssociation vectorialAssociation;
                if (association.PrincipalEnd.Role == fromRole)
                {
                    Debug.Assert(association.DependentEnd.Role == toRole);

                    vectorialAssociation = new(relationship, association.PrincipalEnd, association.DependentEnd);
                }
                else
                {
                    Debug.Assert(association.PrincipalEnd.Role == toRole);
                    Debug.Assert(association.DependentEnd.Role == fromRole);

                    vectorialAssociation = new(relationship, association.DependentEnd, association.PrincipalEnd);
                }

                _path = [vectorialAssociation];
                _fromMultiplicity = vectorialAssociation.FromEnd.Multiplicity;
                _toMultiplicity = vectorialAssociation.ToEnd.Multiplicity;
            }
            else // ManyToMany
            {
                VectorialAssociation vectorialAssociation1, vectorialAssociation2;
                ManyToManyAssociation manyToMany = entityType.EntityDataModel.ManyToManyAssociations[relationship];
                if (manyToMany.FirstAssociation.PrincipalEnd.Role == fromRole)
                {
                    Debug.Assert(manyToMany.SecondAssociation.PrincipalEnd.Role == toRole);

                    vectorialAssociation1 = new(manyToMany.FirstAssociation.Name, manyToMany.FirstAssociation.PrincipalEnd, manyToMany.FirstAssociation.DependentEnd);
                    vectorialAssociation2 = new(manyToMany.SecondAssociation.Name, manyToMany.SecondAssociation.DependentEnd, manyToMany.SecondAssociation.PrincipalEnd);
                }
                else
                {
                    Debug.Assert(manyToMany.FirstAssociation.PrincipalEnd.Role == toRole);
                    Debug.Assert(manyToMany.SecondAssociation.PrincipalEnd.Role == fromRole);

                    vectorialAssociation1 = new(manyToMany.SecondAssociation.Name, manyToMany.SecondAssociation.PrincipalEnd, manyToMany.SecondAssociation.DependentEnd);
                    vectorialAssociation2 = new(manyToMany.FirstAssociation.Name, manyToMany.FirstAssociation.DependentEnd, manyToMany.FirstAssociation.PrincipalEnd);
                }

                _path = [vectorialAssociation1, vectorialAssociation2];
                _fromMultiplicity = Multiplicity.Many;
                _toMultiplicity = Multiplicity.Many;
            }
        }

    }
}
