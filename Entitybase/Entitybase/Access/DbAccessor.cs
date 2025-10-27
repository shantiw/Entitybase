using Shantiw.Data.Meta;
using Shantiw.Data.Querying;
using Shantiw.Data.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Access
{
    public abstract class DbAccessor
    {
        protected abstract string LeftBracket { get; }
        protected abstract string RightBracket { get; }

        protected readonly string ConnectionString;
        protected readonly DbConnection Connection;

        protected abstract DbConnection CreateConnection();
        protected abstract DbDataAdapter CreateDataAdapter();

        public abstract DataSet ExecuteQuery(Query query);

        public virtual XElement ExecuteQueryToDataTableTree()
        {
            // DataTableNode
            throw new NotImplementedException();
        }

        public virtual XElement ExecuteQueryToXElement()
        {
            throw new NotImplementedException();
        }

        public virtual JsonDocument ToJsonDocument()
        {
            throw new NotImplementedException();
        }

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

        protected virtual string BuildSelectSql(QueryBase query)
        {
            StringBuilder sb = new();
            sb.Append(CreateSelectClause(query));
            sb.Append(CreateFromClause(query));
            sb.Append(CreateWhereClause(query));
            sb.Append(CreateOrderByClause(query));
            return sb.ToString();
        }

        protected virtual string CreateSelectClause(QueryBase query)
        {
            List<string> propList = [.. query.Select.Properties];
            foreach (ExpandQuery expandQuery in query.Expands)
            {
                propList.AddRange(expandQuery.NavigationPropertyOfParent.Vector[0].FromEnd.Properties.Select(p => p.Name));
            }

            StringBuilder sb = new();
            sb.Append("SELECT ");
            foreach (string propName in propList.Distinct())
            {
                PropertyBase prop = query.EntityType.ScalarProperties[propName];
                if (prop is Property p)
                {
                    sb.Append('T');
                    sb.Append(query.VectRoot.Id);
                    sb.Append('.');
                    sb.Append(EscapeIdentifier(p.ColumnName));
                    sb.Append(" AS ");
                    sb.Append(EscapeIdentifier(p.Name));
                }
                else if (prop is PrincipalProperty pp)
                {
                    sb.Append('T');
                    sb.Append(query.PrincipalPropertyIds[pp.Name]);
                    sb.Append('.');
                    sb.Append(EscapeIdentifier(pp.PropertyRef.ColumnName));
                    sb.Append(" AS ");
                    sb.Append(EscapeIdentifier(pp.Name));
                }
                else if (prop is ComputedProperty cp)
                {
                    sb.Append(CreateExpressionClause(cp.ExpressionObject, query));
                    sb.Append(" AS ");
                    sb.Append(EscapeIdentifier(cp.Name));
                }
                else
                {
                    Debug.Assert(prop is CalculatedProperty);
                }

                sb.Append(", ");
            }

            return sb.ToString(0, sb.Length - 2);
        }

        protected virtual string CreateFromClause(QueryBase query)
        {
            StringBuilder sb = new();
            sb.Append(" FROM ");
            sb.Append(EscapeIdentifier(query.EntityType.TableName));
            sb.Append(" AS T");
            sb.Append(query.VectRoot.Id);

            Debug.Assert(query.VectRoot.Id == 0);

            AppendJoinClause(sb, query.VectRoot);

            return sb.ToString();
        }

        private void AppendJoinClause(StringBuilder sb, VectNode node)
        {
            foreach (VectNode subNode in node.Children)
            {
                VectAssociation vectAssociation = GetVectAssociation(subNode);

                sb.Append(" LEFT JOIN ");
                sb.Append(EscapeIdentifier(vectAssociation.ToEnd.EntityType.TableName));
                sb.Append(" AS T");
                sb.Append(subNode.Id);
                sb.Append(" ON ");

                for (int i = 0; i < vectAssociation.FromEnd.Properties.Length; i++)
                {
                    sb.Append('T');
                    sb.Append(node.Id);
                    sb.Append('.');
                    sb.Append(EscapeIdentifier(vectAssociation.FromEnd.Properties[i].ColumnName));
                    sb.Append(" = ");
                    sb.Append('T');
                    sb.Append(subNode.Id);
                    sb.Append('.');
                    sb.Append(EscapeIdentifier(vectAssociation.ToEnd.Properties[i].ColumnName));

                    if (i < vectAssociation.FromEnd.Properties.Length - 1)
                        sb.Append(" AND ");
                }

                AppendJoinClause(sb, subNode);
            }
        }

        protected virtual string CreateWhereClause(QueryBase query)
        {
            if (query.Filter == null) return string.Empty;

            return " WHERE " + CreateExpressionClause(query.Filter.ExpressionObject, query);

        }

        protected virtual string CreateOrderByClause(QueryBase query)
        {
            if (query.OrderBy == null) return string.Empty;

            StringBuilder sb = new();
            sb.Append(" ORDER BY ");
            for (int i = 0; i < query.OrderBy.SortOrders.Length; i++)
            {
                SortOrder sortOrder = query.OrderBy.SortOrders[i];
                PropertyBase prop = query.EntityType.ScalarProperties[sortOrder.Property];
                if (prop is Property p)
                {
                    sb.Append('T');
                    sb.Append(query.VectRoot.Id);
                    sb.Append('.');
                    sb.Append(EscapeIdentifier(p.ColumnName));
                }
                else if (prop is PrincipalProperty pp)
                {
                    sb.Append('T');
                    sb.Append(query.PrincipalPropertyIds[pp.Name]);
                    sb.Append('.');
                    sb.Append(EscapeIdentifier(pp.PropertyRef.ColumnName));
                }
                else if (prop is ComputedProperty cp)
                {
                    sb.Append(CreateExpressionClause(cp.ExpressionObject, query));
                }

                if (sortOrder is AscendingOrder)
                {
                    sb.Append(" ASC");
                }
                else if (sortOrder is DescendingOrder)
                {
                    sb.Append(" DESC");
                }

                sb.Append(", ");
            }

            return sb.ToString(0, sb.Length - 2);
        }

        protected virtual string CreateExpressionClause(ExpressionObject expressionObject, QueryBase query)
        {
            string clause = expressionObject.Clause;

            foreach (var pair in expressionObject.PropertyNamePlaceholders)
            {
                PropertyBase prop = expressionObject.EntityType.ScalarProperties[pair.Key];
                if (prop is Property p)
                {
                    clause = clause.Replace(pair.Value, 'T' + query.VectRoot.Id + '.' + EscapeIdentifier(p.ColumnName));
                }
                else if (prop is PrincipalProperty pp)
                {
                    clause = clause.Replace(pair.Value, 'T' + query.PrincipalPropertyIds[pp.Name] + '.' + EscapeIdentifier(pp.PropertyRef.ColumnName));
                }
            }

            foreach (var pair in expressionObject.StringPlaceholders)
            {
                string str = pair.Key;
                // Anti-SQL Injection Measure
                clause = clause.Replace(pair.Value, str);
            }

            return clause;
        }

        protected string EscapeIdentifier(string identifier)
        {
            bool isModified = false;
            string[] strs = identifier.Split('.');
            for (int i = 0; i < strs.Length; i++)
            {
                string str = strs[i];
                if (str.StartsWith(LeftBracket) && str.EndsWith(RightBracket)) continue;
                strs[i] = LeftBracket + str + RightBracket;
                isModified = true;
            }
            return isModified ? string.Join('.', strs) : identifier;
        }

        protected static VectAssociation GetVectAssociation(VectNode vectNode)
        {
            return vectNode.VectAssociation ?? throw new ArgumentNullException(nameof(vectNode), "Is a RootNode.");
        }

    }
}
