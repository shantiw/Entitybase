using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shantiw.Data.Access
{
    public class MySqlAccessor(string connectionString) : DbAccessor(connectionString)
    {
        protected override DbConnection CreateConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        protected override DbDataAdapter CreateDataAdapter()
        {
            return new MySqlDataAdapter();
        }

    }
}
