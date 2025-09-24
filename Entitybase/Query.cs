using Shantiw.Data.DataAnnotations;
using Shantiw.Data.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data
{
    public class Query
    {
        public EntityType EntityType { get; private set; }

        public Select Select { get; private set; }

        public Filter Filter { get; private set; }

        public OrderBy OrderBy { get; private set; }

        public long Top { get; private set; }

        public long Skip { get; private set; }

        public List<Query> Expands { get; private set; }

        public Query()
        {
        }

    }

    public class Select
    {
        public List<Property> Properties = [];
        public List<PrincipalProperty> PrincipalProperties = [];
        public List<ComputedProperty> ComputedProperties = [];
        public List<CalculatedProperty> CalculatedProperties = [];
    }

    public class Filter
    {
        public List<Property> Properties = [];
        public List<PrincipalProperty> PrincipalProperties = [];
    }

    public class OrderBy
    {
        public List<Property> Properties = [];
        public List<PrincipalProperty> PrincipalProperties = [];
    }
}
