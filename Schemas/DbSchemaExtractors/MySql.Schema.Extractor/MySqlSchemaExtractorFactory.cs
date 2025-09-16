using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shantiw.Data.Schema
{
    public static class MySqlSchemaExtractorFactory
    {
        public static DbSchemaExtractor Create(string connectionString)
        {
            return new MySqlSchemaExtractor(connectionString);
        }
    }
}
