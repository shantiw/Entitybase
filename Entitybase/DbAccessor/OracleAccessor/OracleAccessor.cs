using Oracle.ManagedDataAccess.Client;
using Shantiw.Data.Querying;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shantiw.Data.Access
{
    public class OracleAccessor(string connectionString) : DbAccessor(connectionString)
    {
        protected override string LeftBracket => "\"";
        protected override string RightBracket => "\"";

        protected override DbConnection CreateConnection()
        {
            return new OracleConnection(ConnectionString);
        }

        protected override DbDataAdapter CreateDataAdapter()
        {
            return new OracleDataAdapter();
        }

        public override DataSet ExecuteQuery(Query query)
        {
            throw new NotImplementedException();
        }

    }
}
