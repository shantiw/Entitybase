﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.DataObjects;
using XData.Data.Modification;
using XData.Data.Schema;

namespace XData.Data.Objects
{
    // Database.Modification.cs
    public partial class Database<T>
    {
        public event InsertingEventHandler<T> Inserting;
        public event InsertedEventHandler<T> Inserted;
        public event DeletingEventHandler<T> Deleting;
        public event UpdatingEventHandler<T> Updating;

        protected void OnInserting(InsertingEventArgs<T> args)
        {
            Inserting?.Invoke(this, args);
        }

        protected void OnInserted(InsertedEventArgs<T> args)
        {
            Inserted?.Invoke(this, args);
        }

        protected void OnDeleting(DeletingEventArgs<T> args)
        {
            Deleting?.Invoke(this, args);
        }

        protected void OnUpdating(UpdatingEventArgs<T> args)
        {
            Updating?.Invoke(this, args);
        }

        private ModificationGenerator _modificationGenerator = null;
        protected ModificationGenerator ModificationGenerator
        {
            get
            {
                if (_modificationGenerator == null)
                {
                    _modificationGenerator = UnderlyingDatabase.CreateModificationGenerator();
                }
                return _modificationGenerator;
            }
        }

        internal protected int Execute(InsertCommand<T> executeCommand, Modifier<T> modifier)
        {
            // establishing a relationship with parent
            if (executeCommand.ParentRelationship != null)
            {
                for (int i = 0; i < executeCommand.ParentRelationship.Properties.Length; i++)
                {
                    string parentProperty = executeCommand.ParentRelationship.Properties[i];
                    object value = executeCommand.ParentPropertyValues[parentProperty];
                    string property = executeCommand.ParentRelationship.RelatedProperties[i];
                    if (executeCommand.PropertyValues.ContainsKey(property))
                    {
                        executeCommand.PropertyValues[property] = value;
                    }
                    else
                    {
                        executeCommand.PropertyValues.Add(property, value);
                    }
                }
            }

            // Sequence
            foreach (XElement propertySchema in executeCommand.EntitySchema.Elements(SchemaVocab.Property).Where(p => p.Attribute(SchemaVocab.Sequence) != null))
            {
                if (propertySchema.Attribute(SchemaVocab.AutoIncrement) != null && propertySchema.Attribute(SchemaVocab.AutoIncrement).Value == "true") continue;

                string propertyName = propertySchema.Attribute(SchemaVocab.Name).Value;

                // provided by user
                if (executeCommand.PropertyValues.ContainsKey(propertyName))
                {
                    if (executeCommand.PropertyValues[propertyName] != null) continue;
                }

                string sequenceName = propertySchema.Attribute(SchemaVocab.Sequence).Value;

                string seq_sql = ModificationGenerator.GenerateFetchSequenceStatement(sequenceName);
                object sequence = UnderlyingDatabase.ExecuteScalar(seq_sql);

                if (executeCommand.PropertyValues.ContainsKey(propertyName))
                {
                    executeCommand.PropertyValues[propertyName] = sequence;
                }
                else
                {
                    executeCommand.PropertyValues.Add(propertyName, sequence);
                }
                modifier.SetObjectValue(executeCommand.AggregNode, propertyName, sequence);
            }

            // non-ManyToMany
            if (executeCommand.EntitySchema.Attribute(SchemaVocab.Name).Value == executeCommand.Entity)
            {
                // patch up SetDefaultValues
                foreach (KeyValuePair<string, object> pair in executeCommand.PropertyValues)
                {
                    modifier.SetObjectValue(executeCommand.AggregNode, pair.Key, pair.Value);
                }
            }

            // raise inserting event
            InsertingEventArgs<T> args = new InsertingEventArgs<T>(executeCommand.AggregNode, executeCommand.Entity, executeCommand.Schema, executeCommand.Path, executeCommand.Aggreg);
            OnInserting(args);

            // non-ManyToMany
            if (executeCommand.EntitySchema.Attribute(SchemaVocab.Name).Value == executeCommand.Entity)
            {
                // synchronize propertyValues with modified aggregNode OnInserting
                SynchronizePropertyValues(executeCommand, modifier);
            }

            //
            modifier.CheckConstraints(executeCommand);

            // GenerateInsertStatement
            string sql = ModificationGenerator.GenerateInsertStatement(executeCommand.PropertyValues, executeCommand.EntitySchema,
                out IReadOnlyDictionary<string, object> dbParameterValues);
            DbParameter[] dbParameters = UnderlyingDatabase.CreateParameters(dbParameterValues);

            // AutoIncrement
            int affected;
            XElement autoPropertySchema = executeCommand.EntitySchema.Elements(SchemaVocab.Property).FirstOrDefault(p =>
                p.Attribute(SchemaVocab.AutoIncrement) != null && p.Attribute(SchemaVocab.AutoIncrement).Value == "true");
            if (autoPropertySchema == null)
            {
                affected = UnderlyingDatabase.ExecuteSqlCommand(sql, dbParameters);
            }
            else
            {
                affected = UnderlyingDatabase.ExecuteInsertCommand(sql, dbParameters, out object autoIncrementValue);

                string propertyName = autoPropertySchema.Attribute(SchemaVocab.Name).Value;
                if (executeCommand.PropertyValues.ContainsKey(propertyName))
                {
                    executeCommand.PropertyValues[propertyName] = autoIncrementValue;
                }
                else
                {
                    executeCommand.PropertyValues.Add(propertyName, autoIncrementValue);
                }

                // non-ManyToMany
                if (executeCommand.EntitySchema.Attribute(SchemaVocab.Name).Value == executeCommand.Entity)
                {
                    modifier.SetObjectValue(executeCommand.AggregNode, propertyName, autoIncrementValue);
                }
            }

            // raise inserted event
            InsertedEventArgs<T> args1 = new InsertedEventArgs<T>(executeCommand.AggregNode, executeCommand.Entity, executeCommand.Schema, executeCommand.Path, executeCommand.Aggreg);
            OnInserted(args1);

            foreach (SQLStatment statment in args1.After)
            {
                int i = UnderlyingDatabase.ExecuteSqlCommand(statment.Sql, statment.Parameters);
            }

            return affected;
        }

        internal protected int Execute(DeleteCommand<T> executeCommand, Modifier<T> modifier)
        {
            // DeletingEventArgs
            Dictionary<string, object> refetch() => FetchSingleFromDb(executeCommand); // Func<IReadOnlyDictionary<string, object>> refetch = () => FetchSingleFromDb(executeCommand);
            DeletingEventArgs<T> args = new DeletingEventArgs<T>(executeCommand.AggregNode, executeCommand.Entity, executeCommand.Schema, executeCommand.Path, executeCommand.Aggreg)
            {
                Refetch = refetch,
            };

            //
            Dictionary<string, object> relPropertyValues = executeCommand.PropertyValues;
            foreach (DirectRelationship relationship in executeCommand.ChildRelationships)
            {
                XElement relatedEntitySchema = executeCommand.Schema.GetEntitySchema(relationship.RelatedEntity);

                XElement relatedKeySchema = new XElement(relatedEntitySchema.Name);
                Dictionary<string, object> relatedPropertyValues = new Dictionary<string, object>();
                for (int i = 0; i < relationship.Properties.Length; i++)
                {
                    string relatedProperty = relationship.RelatedProperties[i];
                    XElement relatedPropertySchema = relatedEntitySchema.Elements(SchemaVocab.Property).First(p => p.Attribute(SchemaVocab.Name).Value == relatedProperty);
                    relatedKeySchema.Add(relatedPropertySchema);

                    string property = relationship.Properties[i];

                    if (!relPropertyValues.ContainsKey(property) || relPropertyValues[property] == null)
                    {
                        relPropertyValues = args.Refetched;
                    }

                    object value = relPropertyValues[property];
                    relatedPropertyValues.Add(relatedProperty, value);
                }

                bool hasChild = HasChildInDb(relatedPropertyValues, relatedEntitySchema, relatedKeySchema);
                if (hasChild)
                {
                    string relatedEntityName = relatedEntitySchema.Attribute(SchemaVocab.Name).Value;
                    IEnumerable<string> relatedPropertyNames = relatedKeySchema.Elements(SchemaVocab.Property).Select(p => "'" + p.Attribute(SchemaVocab.Name).Value + "'");
                    throw new ConstraintException(string.Format(ErrorMessages.RelationshipKeyConflicted,
                        executeCommand.Entity, GetKeyValueMessage(executeCommand),
                        relatedEntityName, string.Join(",", relatedPropertyNames)));
                }
            }

            // raise deleting event
            OnDeleting(args);

            foreach (SQLStatment statment in args.Before)
            {
                int i = UnderlyingDatabase.ExecuteSqlCommand(statment.Sql, statment.Parameters);
            }

            string sql = ModificationGenerator.GenerateDeleteStatement(executeCommand.PropertyValues, executeCommand.EntitySchema,
                executeCommand.UniqueKeySchema, executeCommand.ConcurrencySchema, out IReadOnlyDictionary<string, object> dbParameterValues);
            DbParameter[] dbParameters = UnderlyingDatabase.CreateParameters(dbParameterValues);
            int affected = UnderlyingDatabase.ExecuteSqlCommand(sql, dbParameters);

            if (affected > 1) throw new SQLStatmentException(string.Format(ErrorMessages.MultipleRowsAffected, affected), sql, dbParameters);
            if (affected == 0 && executeCommand.ConcurrencySchema != null && args.RefetchedPropertyValues != null)
            {
                throw new OptimisticConcurrencyException(string.Format(ErrorMessages.OptimisticConcurrencyException,
                    executeCommand.Entity, GetKeyValueMessage(executeCommand)), sql, dbParameters);
            }

            return affected;
        }

        internal protected int Execute(UpdateCommand<T> executeCommand, Modifier<T> modifier)
        {
            UpdatingEventArgs<T> args = new UpdatingEventArgs<T>(executeCommand.AggregNode, executeCommand.Entity, executeCommand.Schema, executeCommand.Path, executeCommand.Aggreg)
            {
                Refetch = () => FetchSingleFromDb(executeCommand)
            };
            OnUpdating(args);

            // non-ManyToMany
            if (executeCommand.EntitySchema.Attribute(SchemaVocab.Name).Value == executeCommand.Entity)
            {
                // synchronize propertyValues with modified aggregNode OnUpdating
                SynchronizePropertyValues(executeCommand, modifier);

                foreach (KeyValuePair<string, object> pair in executeCommand.FixedUpdatePropertyValues)
                {
                    string propertyName = pair.Key;
                    object propertyValue = pair.Value;
                    if (executeCommand.PropertyValues.ContainsKey(propertyName))
                    {
                        object value = executeCommand.PropertyValues[propertyName];
                        if (object.Equals(propertyValue, value)) continue;

                        throw new ConstraintException(string.Format(ErrorMessages.NotChangeFixedValue, propertyName, executeCommand.Entity));
                    }
                }
            }

            //
            modifier.CheckConstraints(executeCommand);

            //
            foreach (SQLStatment statment in args.Before)
            {
                int i = UnderlyingDatabase.ExecuteSqlCommand(statment.Sql, statment.Parameters);
            }

            //
            Dictionary<string, object> updatePropertyValues = new Dictionary<string, object>(executeCommand.FixedUpdatePropertyValues);
            foreach (KeyValuePair<string, object> propertyValue in executeCommand.PropertyValues)
            {
                string property = propertyValue.Key;
                object value = propertyValue.Value;

                if (executeCommand.FixedUpdatePropertyValues.ContainsKey(property)) continue;
                if (executeCommand.UniqueKeySchema.Elements(SchemaVocab.Property).Any(p => p.Attribute(SchemaVocab.Name).Value == property)) continue;
                if (executeCommand.EntitySchema.Elements(SchemaVocab.Property).Any(p => p.Attribute(SchemaVocab.Name).Value == property &&
                    p.Attribute(SchemaVocab.Readonly) != null && p.Attribute(SchemaVocab.Readonly).Value == "true")) continue;

                updatePropertyValues.Add(propertyValue.Key, propertyValue.Value);
            }

            //
            string sql = ModificationGenerator.GenerateUpdateStatement(executeCommand.PropertyValues, updatePropertyValues,
                executeCommand.EntitySchema, executeCommand.UniqueKeySchema, executeCommand.ConcurrencySchema,
                out IReadOnlyDictionary<string, object> dbParameterValues);
            DbParameter[] dbParameters = UnderlyingDatabase.CreateParameters(dbParameterValues);
            int affected = UnderlyingDatabase.ExecuteSqlCommand(sql, dbParameters);

            if (affected > 1) throw new SQLStatmentException(string.Format(ErrorMessages.MultipleRowsAffected, affected), sql, dbParameters);
            if (affected == 0)
            {
                if (executeCommand.ConcurrencySchema != null && args.RefetchedPropertyValues != null)
                {
                    throw new OptimisticConcurrencyException(string.Format(ErrorMessages.OptimisticConcurrencyException,
                         executeCommand.Entity, GetKeyValueMessage(executeCommand)), sql, dbParameters);
                }

                throw new SQLStatmentException(ErrorMessages.DeletedByAnotherUser, sql, dbParameters);
            }

            //
            foreach (SQLStatment statment in args.After)
            {
                int i = UnderlyingDatabase.ExecuteSqlCommand(statment.Sql, statment.Parameters);
            }

            return affected;
        }

        private void SynchronizePropertyValues(ExecuteCommand<T> executeCommand, Modifier<T> modifier)
        {
            // Assert(executeCommand.GetType() == typeof(InsertCommand<T>) || executeCommand.GetType() == typeof(UpdateCommand<T>));

            Dictionary<string, object> dict = modifier.GetPropertyValues(executeCommand.AggregNode, executeCommand.EntitySchema);
            foreach (KeyValuePair<string, object> pair in dict)
            {
                string propertyName = pair.Key;
                object propertyvalue = pair.Value;

                if (executeCommand.PropertyValues.ContainsKey(propertyName))
                {
                    object value = executeCommand.PropertyValues[propertyName];

                    //
                    XElement uniqueKeyPropertySchema = executeCommand.UniqueKeySchema.Elements(SchemaVocab.Property)
                        .FirstOrDefault(p => p.Attribute(SchemaVocab.Name).Value == propertyName);
                    if (uniqueKeyPropertySchema != null)
                    {
                        if (value != null)
                        {
                            if (object.Equals(propertyvalue, value)) continue;

                            throw new ConstraintException(string.Format(ErrorMessages.NotChangeUniqueKeyValue, propertyName, executeCommand.Entity));
                        }
                    }

                    //
                    XElement propertySchema = executeCommand.EntitySchema.Elements(SchemaVocab.Property)
                       .First(p => p.Attribute(SchemaVocab.Name).Value == propertyName);
                    if (propertySchema.Attribute(SchemaVocab.Readonly) != null && propertySchema.Attribute(SchemaVocab.Readonly).Value == "true")
                    {
                        if (object.Equals(propertyvalue, value)) continue;

                        throw new ConstraintException(string.Format(ErrorMessages.NotChangeReadonlyValue, propertyName, executeCommand.Entity));
                    }

                    executeCommand.PropertyValues[propertyName] = propertyvalue;
                }
                else
                {
                    executeCommand.PropertyValues.Add(propertyName, propertyvalue);
                }
            }
        }

        private string GetKeyValueMessage(ExecuteCommand<T> executeCommand)
        {
            // Assert(executeCommand.GetType() == typeof(DeleteCommand<T>) || executeCommand.GetType() == typeof(UpdateCommand<T>));

            List<string> strings = new List<string>();
            foreach (XElement propertySchema in executeCommand.UniqueKeySchema.Elements(SchemaVocab.Property))
            {
                string propertyName = propertySchema.Attribute(SchemaVocab.Name).Value;
                strings.Add("'" + executeCommand.PropertyValues[propertyName].ToString() + "'");
            }
            return string.Join(",", strings);
        }

        private Dictionary<string, object> FetchSingleFromDb(ExecuteCommand<T> executeCommand)
        {
            // Assert(executeCommand.GetType() == typeof(DeleteCommand<T>) || executeCommand.GetType() == typeof(UpdateCommand<T>));

            IEnumerable<Dictionary<string, object>> refetchedRecords = FetchFromDb(executeCommand.PropertyValues, executeCommand.EntitySchema, executeCommand.UniqueKeySchema);
            int count = refetchedRecords.Count();
            if (count > 1) throw new ConstraintException(string.Format(ErrorMessages.MultipleRowsFound, count, executeCommand.Entity, GetKeyValueMessage(executeCommand)));

            return refetchedRecords.FirstOrDefault();
        }

        protected virtual IEnumerable<Dictionary<string, object>> FetchFromDb(Dictionary<string, object> propertyValues,
            XElement entitySchema, XElement keySchema)
        {
            string sql = ModificationGenerator.GenerateFetchStatement(propertyValues, entitySchema, keySchema,
                out IReadOnlyDictionary<string, object> dbParameterValues);
            DbParameter[] dbParameters = UnderlyingDatabase.CreateParameters(dbParameterValues);
            DataTable table = UnderlyingDatabase.ExecuteDataTable(sql, dbParameters);

            List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
            foreach (DataRow row in table.Rows)
            {
                Dictionary<string, object> dict = new Dictionary<string, object>();
                foreach (DataColumn column in table.Columns)
                {
                    XElement propertySchema = entitySchema.Elements(SchemaVocab.Property).First(p => p.Attribute(SchemaVocab.Column).Value == column.ColumnName);
                    string propertyName = propertySchema.Attribute(SchemaVocab.Name).Value;
                    dict.Add(propertyName, row[column]);
                }
                list.Add(dict);
            }

            return list;
        }

        protected virtual bool HasChildInDb(Dictionary<string, object> propertyValues, XElement entitySchema, XElement keySchema)
        {
            string sql = ModificationGenerator.GenerateHasChildStatement(propertyValues, entitySchema, keySchema,
                 out IReadOnlyDictionary<string, object> dbParameterValues);
            DbParameter[] dbParameters = UnderlyingDatabase.CreateParameters(dbParameterValues);
            object obj = UnderlyingDatabase.ExecuteScalar(sql, dbParameters);
            return obj != null;
        }

        //
        internal protected int Execute(UpdateCommandNode<T> node, Modifier<T> modifier)
        {
            if (node.Original != null) return Execute_Original(node, modifier);

            List<DeleteCommand<T>> deleteCommands = new List<DeleteCommand<T>>();
            int affected = Execute(node, deleteCommands, modifier);

            deleteCommands.Reverse();
            foreach (DeleteCommand<T> deleteCommand in deleteCommands)
            {
                Execute(deleteCommand, modifier);
            }

            return affected;
        }

        private int Execute(UpdateCommandNode<T> node, List<DeleteCommand<T>> deleteCommands, Modifier<T> modifier)
        {
            int affected = Execute(node as UpdateCommand<T>, modifier);
            if (node.ChildrenCollection.Count == 0) return affected;

            //
            foreach (UpdateCommandNodeChildren<T> nodeChildren in node.ChildrenCollection)
            {
                DirectRelationship relationship = nodeChildren.ParentRelationship;
                string childrenPath = nodeChildren.Path;
                ICollection<UpdateCommandNode<T>> childNodes = nodeChildren.UpdateCommandNodes;

                Dictionary<string, object> relatedPropertyValues = GetRelatedPropertyValues(relationship, node, out Dictionary<string, object> dbPropertyValues);

                // establishing relationship
                foreach (UpdateCommandNode<T> childNode in childNodes)
                {
                    foreach (KeyValuePair<string, object> propertyValue in relatedPropertyValues)
                    {
                        if (!childNode.FixedUpdatePropertyValues.ContainsKey(propertyValue.Key))
                        {
                            childNode.FixedUpdatePropertyValues.Add(propertyValue.Key, propertyValue.Value);
                        }
                    }
                }

                IEnumerable<IReadOnlyDictionary<string, object>> relatedRecords = FetchRelatedFromDb(
                    relatedPropertyValues, relationship.RelatedEntity, node.Schema);

                // decide
                List<IReadOnlyDictionary<string, object>> refetchedChildren = new List<IReadOnlyDictionary<string, object>>(relatedRecords);

                string childEntity = relationship.RelatedEntity;
                XElement childEntitySchema = node.Schema.GetEntitySchema(childEntity);
                XElement childKeySchema = SchemaHelper.GetKeySchema(childEntitySchema);
                XElement childConcurrencySchema = SchemaHelper.GetConcurrencySchema(childEntitySchema);
                IEnumerable<DirectRelationship> childRelationships = node.Schema.GetDirectRelationships(childEntity);

                List<UpdateCommandNode<T>> childUpdateCommandNodes = new List<UpdateCommandNode<T>>();
                foreach (UpdateCommandNode<T> childNode in childNodes)
                {
                    Dictionary<string, object> childKeyPropertyValues = new Dictionary<string, object>();
                    foreach (XElement childPropertySchema in childNode.UniqueKeySchema.Elements(SchemaVocab.Property))
                    {
                        string childPropertyName = childPropertySchema.Attribute(SchemaVocab.Name).Value;
                        object value = (childNode.PropertyValues.ContainsKey(childPropertyName)) ? childNode.PropertyValues[childPropertyName] : null;
                        childKeyPropertyValues.Add(childPropertyName, value);
                    }

                    IReadOnlyDictionary<string, object> found = Find(refetchedChildren, childKeyPropertyValues);
                    if (found == null)
                    {
                        if (childConcurrencySchema != null)
                        {
                            bool hasValue = false;
                            foreach (XElement propertySchema in childConcurrencySchema.Elements())
                            {
                                string property = propertySchema.Attribute(SchemaVocab.Name).Value;
                                if (childNode.PropertyValues.ContainsKey(property))
                                {
                                    object value = childNode.PropertyValues[property];
                                    if (value != null && value.ToString() != string.Empty)
                                    {
                                        hasValue = true;
                                        break;
                                    }
                                }
                            }

                            if (hasValue)
                            {
                                throw new SQLStatmentException(ErrorMessages.DeletedByAnotherUser,
                                    string.Format("insert '{0}' into '{1}'", childNode.AggregNode.ToString(),
                                        childNode.EntitySchema.Attribute(SchemaVocab.Collection).Value));
                            }
                        }

                        Insert(childNode, relationship, dbPropertyValues, modifier);
                    }
                    else
                    {
                        childUpdateCommandNodes.Add(childNode);
                        refetchedChildren.Remove(found);
                    }
                }

                //
                int index = -1;
                foreach (Dictionary<string, object> childPropertyValues in refetchedChildren)
                {
                    T aggregNode = modifier.CreateObject(childPropertyValues, childEntity);
                    DeleteCommand<T> deleteCommand = new DeleteCommand<T>(aggregNode, childEntity, node.Schema, node.Aggreg)
                    {
                        EntitySchema = childEntitySchema,
                        UniqueKeySchema = childKeySchema,
                        ConcurrencySchema = childConcurrencySchema,
                        ChildRelationships = childRelationships,
                        PropertyValues = childPropertyValues,
                        Path = string.Format("{0}[{1}]", childrenPath, index)
                    };

                    deleteCommands.Add(deleteCommand);

                    index--;
                }

                // constraint violation: missing ConcurrencyCheck value(s)
                if (childConcurrencySchema != null && refetchedChildren.Count > 0)
                {
                    throw new OptimisticConcurrencyException(string.Format(ErrorMessages.OptimisticConcurrencyException,
                        deleteCommands[0].Entity, GetKeyValueMessage(deleteCommands[0])), "DELETE...");
                }

                //
                foreach (UpdateCommandNode<T> childUpdateCommandNode in childUpdateCommandNodes)
                {
                    Execute(childUpdateCommandNode, deleteCommands, modifier);
                }
            }

            return affected;
        }

        private Dictionary<string, object> GetRelatedPropertyValues(DirectRelationship relationship, UpdateCommandNode<T> parent, out Dictionary<string, object> dbParentPropertyValues)
        {
            dbParentPropertyValues = parent.PropertyValues;
            Dictionary<string, object> relatedPropertyValues = new Dictionary<string, object>();
            for (int i = 0; i < relationship.Properties.Length; i++)
            {
                string propertyName = relationship.Properties[i];
                string relatedPropertyName = relationship.RelatedProperties[i];

                if (!dbParentPropertyValues.ContainsKey(propertyName) || dbParentPropertyValues[propertyName] == null)
                {
                    dbParentPropertyValues = FetchFromDb(parent.PropertyValues, parent.EntitySchema, parent.UniqueKeySchema).First();

                }

                object value = dbParentPropertyValues[propertyName];
                relatedPropertyValues.Add(relatedPropertyName, value);
            }

            return relatedPropertyValues;
        }

        protected virtual IEnumerable<IReadOnlyDictionary<string, object>> FetchRelatedFromDb(Dictionary<string, object> relatedPropertyValues,
            string relatedEntity, XElement schema)
        {
            XElement entitySchema = schema.GetEntitySchema(relatedEntity);
            XElement keySchema = new XElement(entitySchema);
            keySchema.RemoveNodes();
            foreach (KeyValuePair<string, object> propertyValue in relatedPropertyValues)
            {
                string propertyName = propertyValue.Key;
                XElement keyPropertySchema = entitySchema.Elements(SchemaVocab.Property).First(p => p.Attribute(SchemaVocab.Name).Value == propertyName);
                keySchema.Add(keyPropertySchema);
            }

            return FetchFromDb(relatedPropertyValues, entitySchema, keySchema);
        }

        private IReadOnlyDictionary<string, object> Find(IEnumerable<IReadOnlyDictionary<string, object>> refetched, Dictionary<string, object> keyPropertyValues)
        {
            IEnumerable<IReadOnlyDictionary<string, object>> result = refetched;
            foreach (KeyValuePair<string, object> pair in keyPropertyValues)
            {
                result = refetched.Where(p => pair.Value != null && p[pair.Key].ToString() == pair.Value.ToString());
            }
            return result.FirstOrDefault();
        }

        private void Insert(UpdateCommandNode<T> node, DirectRelationship relationship, Dictionary<string, object> parentPropertyValues, Modifier<T> modifier)
        {
            InsertCommand<T> insertCommand = new InsertCommand<T>(node.AggregNode, node.Entity, node.Schema, node.Aggreg)
            {
                EntitySchema = node.EntitySchema,
                UniqueKeySchema = node.UniqueKeySchema,
                PropertyValues = node.PropertyValues,
                Path = node.Path,
                ParentPropertyValues = parentPropertyValues,
                ParentRelationship = relationship
            };

            SchemaHelper.SetDefaultValues(insertCommand.PropertyValues, insertCommand.EntitySchema);

            Execute(insertCommand, modifier);

            foreach (UpdateCommandNodeChildren<T> nodeChildren in node.ChildrenCollection)
            {
                DirectRelationship childRelationship = nodeChildren.ParentRelationship;
                ICollection<UpdateCommandNode<T>> childNodes = nodeChildren.UpdateCommandNodes;

                foreach (UpdateCommandNode<T> childNode in childNodes)
                {
                    Insert(childNode, childRelationship, insertCommand.PropertyValues, modifier);
                }
            }
        }


    }
}
