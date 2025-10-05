using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shantiw.Data.Meta
{
    public class VectAssociation
    {
        public string Name { get; private set; }

        public VectAssociationEnd FromEnd { get; private set; }

        public VectAssociationEnd ToEnd { get; private set; }

        internal VectAssociation(string name, AssociationEnd fromEnd, AssociationEnd toEnd)
        {
            Name = $"{fromEnd.Role}_{toEnd.Role}_{name}";
            FromEnd = new(this, fromEnd);
            ToEnd = new(this, toEnd);
        }

    }
}
