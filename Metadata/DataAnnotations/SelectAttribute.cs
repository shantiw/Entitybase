using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shantiw.Data.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class SelectAttribute(string defaultValue) : StorageAttribute
    {
        public string[] Default { get; private set; } = [.. defaultValue.Split(',', StringSplitOptions.TrimEntries)];
    }
}
