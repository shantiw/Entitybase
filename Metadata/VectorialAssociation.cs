using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shantiw.Data.Meta
{
    public class VectorialAssociation
    {
        public string Name { get; private set; }

        public VectorialAssociationEnd FromEnd { get; private set; }

        public VectorialAssociationEnd ToEnd { get; private set; }

        internal VectorialAssociation(string name, AssociationEnd fromEnd, AssociationEnd toEnd)
        {
            Name = name;
            FromEnd = new(this, fromEnd);
            ToEnd = new(this, toEnd);
        }

    }
}
