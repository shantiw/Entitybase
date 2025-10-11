using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shantiw.Data.Access
{
    public class OracleAccessor(string connectionString) : DbAccessor(connectionString)
    {
        protected override DbConnection CreateConnection()
        {
            return new OracleConnection(ConnectionString);
        }

        protected override DbDataAdapter CreateDataAdapter()
        {
            return new OracleDataAdapter();
        }

    }
}
