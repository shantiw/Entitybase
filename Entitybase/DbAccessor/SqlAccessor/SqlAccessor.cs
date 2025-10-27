using Microsoft.Data.SqlClient;
using Shantiw.Data.Meta;
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
    public class SqlAccessor(string connectionString) : DbAccessor(connectionString)
    {
        protected override string LeftBracket => "[";
        protected override string RightBracket => "]";

        protected override DbConnection CreateConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        protected override DbDataAdapter CreateDataAdapter()
        {
            return new SqlDataAdapter();
        }

        public override DataSet ExecuteQuery(Query query)
        {
            throw new NotImplementedException();
        }



    }
}
