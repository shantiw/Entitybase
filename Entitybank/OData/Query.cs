﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Schema;

namespace XData.Data.OData
{
    public class Query
    {
        public string Entity { get; private set; }
        public XElement Schema { get; private set; }

        public Select Select { get; private set; }
        public Filter Filter { get; private set; }
        public Orderby Orderby { get; private set; }
        public long Skip { get; private set; }
        public long Top { get; private set; }

        public IEnumerable<Property> Properties { get; private set; }
        public IEnumerable<string> Parameters { get; private set; }

        public ParameterCollection ParameterCollection { get; private set; }

        internal IReadOnlyDictionary<string, object> ParameterValues { get => ParameterCollection.ParameterValues; }

        internal IReadOnlyDictionary<string, string> UpperParamNameMapping { get => ParameterCollection.UpperNameMapping; }

        internal Func<string> GenerateNextParamName { get => ParameterCollection.GenerateNextParamName; }

        public Query(string entity, string select, string filter, string orderby, XElement schema, ParameterCollection parameterCollection)
        {
            Entity = entity;
            Schema = new XElement(schema);
            ParameterCollection = parameterCollection;

            //
            Select = new Select(select, Entity, Schema);
            List<string> parameterList = new List<string>();
            IEnumerable<string> properties = Select.Properties;
            if (!string.IsNullOrWhiteSpace(filter))
            {
                Filter = new Filter(filter, Entity, Schema);
                properties = properties.Union(Filter.Properties);
                parameterList.AddRange(Filter.Parameters);
            }
            if (!string.IsNullOrWhiteSpace(orderby))
            {
                Orderby = new Orderby(orderby, Entity, Schema);
                properties = properties.Union(Orderby.Orders.Select(o => o.Property));
            }

            //
            List<Property> propertyList = new List<Property>();
            XElement entitySchema = Schema.GetEntitySchema(Entity);
            foreach (string property in properties)
            {
                Property oProperty;

                XElement propertySchema = entitySchema.Elements(SchemaVocab.Property).FirstOrDefault(x => x.Attribute(SchemaVocab.Name).Value == property);
                if (propertySchema == null)
                {
                    if (!property.Contains(".")) throw new SyntaxErrorException(string.Format(ErrorMessages.NotFoundProperty, property, Entity));

                    oProperty = ExtendProperty.GenerateExtendProperty(property, Entity, schema);
                }
                else
                {
                    oProperty = FieldProperty.Create(property, entity, Schema);
                }

                propertyList.Add(oProperty);
            }

            Properties = propertyList;

            Parameters = parameterList.Distinct();

            ParameterCollection.AddRange(Parameters);
        }

        public Query(string entity, string select, string filter, string orderby, long skip, long top, XElement schema,
            ParameterCollection parameterCollection)
         : this(entity, select, filter, orderby, schema, parameterCollection)
        {
            Skip = skip;
            Top = top;
        }


    }
}