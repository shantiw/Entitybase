using Shantiw.Data.Schema;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Meta
{
    public partial class EntityDataModel
    {
        public IReadOnlyDictionary<string, EntityType> EntityTypes { get; private set; }

        internal IReadOnlyDictionary<string, Association> Associations { get; private set; }

        internal IReadOnlyDictionary<string, ManyToManyAssociation> ManyToManyAssociations { get; private set; }

        internal EntityDataModel(XElement schema)
        {
            Dictionary<string, EntityType> entityTypes = [];
            foreach (XElement xEntityType in schema.Elements(SchemaVocab.EntityType))
            {
                EntityType entityType = new(this, xEntityType);
                entityTypes.Add(entityType.Name, entityType);
            }
            EntityTypes = entityTypes;

            //
            Dictionary<string, Association> associations = [];
            Dictionary<string, ManyToManyAssociation> manyToManyAssociations = [];
            foreach (XElement xAssociation in schema.Elements(SchemaVocab.Association))
            {
                if (xAssociation.Elements(SchemaVocab.End).All(p => p.GetAttributeValue(nameof(Multiplicity)) == Multiplicity.Many))
                {
                    ManyToManyAssociation manyToManyAssociation = new(xAssociation, EntityTypes);
                    manyToManyAssociations.Add(manyToManyAssociation.Name, manyToManyAssociation);
                }
                else
                {
                    Association association = new(xAssociation, EntityTypes);
                    associations.Add(association.Name, association);
                }
            }
            Associations = associations;
            ManyToManyAssociations = manyToManyAssociations;

            // <NavigationProperty Name="..." Relationship="..." FromRole="..." ToRole="..." />
            foreach (EntityType entityType in EntityTypes.Values)
            {
                entityType.BuildRelationshipNavigationProperties();
            }

            // <PrincipalProperty... />
            // <NavigationProperty Name="..." Route="..." />
            foreach (EntityType entityType in EntityTypes.Values)
            {
                entityType.BuildPrincipalAndRouteNavigationProperties();
            }
        }

    }
}
