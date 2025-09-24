using Shantiw.Data.Meta;
using System.Xml.Linq;

namespace Shantiw.Services
{
    public class ODataService
    {
        public ODataService()
        {
            var factory = new EntityDataModelFactory(XElement.Load("northwind.xml"));
            var mode = factory.GetInstance();
        }
    }
}
