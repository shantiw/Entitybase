﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.DataObjects;

namespace XData.Data.Objects
{
    public abstract class ExecuteEventArgs<T> : EventArgs
    {
        public T AggregNode { get; private set; }
        public string Entity { get; private set; }
        public T Aggreg { get; private set; }
        public XElement Schema { get; private set; }
        public string Path { get; private set; }

        protected ExecuteEventArgs(T aggregNode, string entity, XElement schema, string path, T aggreg)
        {
            AggregNode = aggregNode;
            Entity = entity;
            Schema = schema;
            Path = path;
            Aggreg = aggreg;
        }
    }

    public class InsertingEventArgs<T> : ExecuteEventArgs<T>
    {
        public InsertingEventArgs(T aggregNode, string entity, XElement schema, string path, T aggreg)
            : base(aggregNode, entity, schema, path, aggreg)
        {
        }
    }

    public class InsertedEventArgs<T> : ExecuteEventArgs<T>
    {
        // sql, parameters
        public IList<SQLStatment> After { get; private set; } = new List<SQLStatment>();

        public InsertedEventArgs(T aggregNode, string entity, XElement schema, string path, T aggreg)
            : base(aggregNode, entity, schema, path, aggreg)
        {
        }
    }

    public class DeletingEventArgs<T> : ExecuteEventArgs<T>
    {
        public IReadOnlyDictionary<string, object> Refetched { get; internal set; }

        // sql, parameters
        public IList<SQLStatment> Before { get; private set; } = new List<SQLStatment>();

        public DeletingEventArgs(T aggregNode, string entity, XElement schema, string path, T aggreg)
            : base(aggregNode, entity, schema, path, aggreg)
        {
        }
    }

    public class UpdatingEventArgs<T> : ExecuteEventArgs<T>
    {
        //internal Func<IReadOnlyDictionary<string, object>> Refetch;
        internal Func<IReadOnlyDictionary<string, object>> Refetch = null;
        public IReadOnlyDictionary<string, object> Refetched { get => Refetch.Invoke(); }

        // sql, parameters
        public IList<SQLStatment> Before { get; private set; } = new List<SQLStatment>();

        // sql, parameters
        public IList<SQLStatment> After { get; private set; } = new List<SQLStatment>();

        public UpdatingEventArgs(T aggregNode, string entity, XElement schema, string path, T aggreg)
            : base(aggregNode, entity, schema, path, aggreg)
        {
        }
    }

    public delegate void InsertingEventHandler<T>(object sender, InsertingEventArgs<T> args);
    public delegate void InsertedEventHandler<T>(object sender, InsertedEventArgs<T> args);
    public delegate void DeletingEventHandler<T>(object sender, DeletingEventArgs<T> args);
    public delegate void UpdatingEventHandler<T>(object sender, UpdatingEventArgs<T> args);

}
