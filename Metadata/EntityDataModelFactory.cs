using Shantiw.Data.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Meta
{
    public class EntityDataModelFactory(XElement schema)
    {
        private static class Method
        {
            public const string Add = "Add";
            public const string Remove = "Remove";
            public const string Replace = "Replace";
        }

        private readonly XElement _schema = schema;
        private readonly EntityDataModel _defaultModel = new(schema);

        public EntityDataModel GetInstance()
        {
            return _defaultModel;
        }

        /// <param name="xSML">schema manipulation/modification language</param>
        public EntityDataModel Create(XElement xSML)
        {
            XElement schema = new(_schema);

            foreach (XElement xMethod in xSML.Elements())
            {
                string name = xMethod.Name.LocalName;
                if (name == Method.Add)
                {
                    Add(schema, xMethod);
                }
                else if (name == Method.Remove)
                {
                    Remove(schema, xMethod);
                }
                else if (name == Method.Replace)
                {
                    Replace(schema, xMethod);
                }
            }

            return new(schema);
        }

        private static void Add(XElement schema, XElement xAdd)
        {
            foreach (XElement mEntityType in xAdd.Elements(SchemaVocab.EntityType))
            {
                XElement xEntityType = GetXEntityType(mEntityType, schema);

                foreach(XElement mProperty in mEntityType.Elements(SchemaVocab.Property))
                {

                }

                foreach (XElement mAnnotation in mEntityType.Elements(SchemaVocab.Annotation))
                {

                }
            }
        }

        private static void Remove(XElement schema, XElement xRemove)
        {
            foreach (XElement mEntityType in xRemove.Elements(SchemaVocab.EntityType))
            {
                XElement xEntityType = GetXEntityType(mEntityType, schema);
            }
        }

        private static void Replace(XElement schema, XElement xReplace)
        {
            foreach (XElement mEntityType in xReplace.Elements(SchemaVocab.EntityType))
            {
                XElement xEntityType = GetXEntityType(mEntityType, schema);
            }
        }

        private static XElement GetXEntityType(XElement mEntityType, XElement schema)
        {
            string name = mEntityType.GetAttributeValue(SchemaVocab.Name);
            return schema.Elements(SchemaVocab.EntityType).Single(e => e.GetAttributeValue(SchemaVocab.Name) == name);
        }

        private static XElement? GetNullableXProperty(XElement mProperty, XElement xEntityType)
        {
            string name = mProperty.GetAttributeValue(SchemaVocab.Name);
            xEntityType.Elements(SchemaVocab.Property).SingleOrDefault(e => e.GetAttributeValue(SchemaVocab.Name) == name);
        }

    }
}
