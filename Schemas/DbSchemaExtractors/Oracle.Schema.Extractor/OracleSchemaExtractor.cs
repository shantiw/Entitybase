using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace Shantiw.Data.Schema
{
    // Oracle 10.2.0.4.0
    // not supported: LONG, LONG VARCHAR, LONG RAW
    internal class OracleSchemaExtractor(string connectionString) : DbSchemaExtractor(connectionString)
    {
        private const string PROVIDER = "Oracle.ManagedDataAccess.Client";

        protected override DbConnection CreateConnection()
        {
            return new OracleConnection(ConnectionString);
        }

        protected override DbDataAdapter CreateDataAdapter()
        {
            return new OracleDataAdapter();
        }

        protected override Database GetDatabase()
        {
            throw new NotImplementedException();
        }
    }
}
