using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shantiw.Data.Meta
{
    public class VectorialAssociationEnd
    {
        public VectorialAssociation VectorialAssociation { get; private set; }

        public string Role { get; private set; }

        public string Multiplicity { get; private set; }

        public EntityType EntityType { get; private set; }

        public Property[] Properties { get; private set; }

        internal VectorialAssociationEnd(VectorialAssociation vectorialAssociation, AssociationEnd associationEnd)
        {
            VectorialAssociation = vectorialAssociation;
            Role = associationEnd.Role;
            Multiplicity = associationEnd.Multiplicity;
            EntityType = associationEnd.EntityType;
            Properties = associationEnd.Properties;
        }

    }
}
