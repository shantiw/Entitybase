using Shantiw.Data.DataAnnotations;
using Shantiw.Data.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.OData
{
    public class Query
    {
        public EntityType EntityType { get; set; }
        public string Select;
        public string Filter;
        public string OrderBy;

        public long Skip { get; private set; }
        public long Top { get; private set; }

        public Query Expand { get; private set; }

        public Query()
        {
            var factory = new EntityDataModelFactory(XElement.Load("northwind.xml"));
            var mode = factory.GetInstance();
        }

    }
}
