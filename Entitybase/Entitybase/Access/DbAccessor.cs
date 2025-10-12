using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shantiw.Data.Access
{
    public abstract class DbAccessor
    {
        protected readonly string ConnectionString;
        protected readonly DbConnection Connection;

        protected abstract DbConnection CreateConnection();
        protected abstract DbDataAdapter CreateDataAdapter();

        protected DbAccessor(string connectionString)
        {
            ConnectionString = connectionString;
            Connection = CreateConnection();
        }

        protected DataTable CreateDataTable(string sql)
        {
            DbCommand command = Connection.CreateCommand();
            command.CommandText = sql;
            DbDataAdapter adapter = CreateDataAdapter();
            adapter.SelectCommand = command;
            DataTable table = new();
            adapter.Fill(table);
            return table;
        }

    }
}
