using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shantiw.Data.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class UpdateAttribute : StorageAttribute
    {
        public string Where { get; private set; }

        public UpdateAttribute(string where)
        {
            Where = where;
        }
    }
}
