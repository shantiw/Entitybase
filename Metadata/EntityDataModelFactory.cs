using Shantiw.Data.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
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
            }

            return new(schema);
        }

        private static void Add(XElement schema, XElement xAdd)
        {
            foreach (XElement mEntityType in xAdd.Elements(SchemaVocab.EntityType))
            {
                XElement xEntityType = FindXEntityType(mEntityType, schema);

                foreach (XElement mProperty in mEntityType.Elements(SchemaVocab.Property))
                {
                    string name = mProperty.GetAttributeValue(SchemaVocab.Name);
                    XElement? xProperty = FindXProperty(name, xEntityType)
                        ?? throw new ArgumentException($"The named \"{name}\" property is not found in Schema.");

                    if (xProperty.Name.LocalName != SchemaVocab.Property)
                        throw new ArgumentException($"The named \"{name}\" Xproperty is not a Property.");

                    AddAnnotations(xProperty, mProperty);
                }

                foreach (XElement mPrincipalProperty in mEntityType.Elements(MetaVocab.PrincipalProperty))
                {
                    string name = mPrincipalProperty.GetAttributeValue(SchemaVocab.Name);
                    XElement? xProperty = FindXProperty(name, xEntityType);
                    if (xProperty == null)
                    {
                        xEntityType.Add(mPrincipalProperty);
                    }
                    else
                    {
                        if (xProperty.Name.LocalName != MetaVocab.PrincipalProperty)
                            throw new ArgumentException($"The named \"{name}\" Xproperty is not a PrincipalProperty.");

                        AddAnnotations(xProperty, mPrincipalProperty);
                    }
                }

                foreach (XElement mNavigationProperty in mEntityType.Elements(SchemaVocab.NavigationProperty))
                {
                    string name = mNavigationProperty.GetAttributeValue(SchemaVocab.Name);
                    XElement? xProperty = FindXProperty(name, xEntityType);
                    if (xProperty == null)
                    {
                        if (mNavigationProperty.GetNullableAttributeValue(MetaVocab.Route) == null)
                            throw new ArgumentException($"The \"Route\" attribute is not found in {mNavigationProperty}.");

                        xEntityType.Add(mNavigationProperty);
                    }
                    else
                    {
                        if (xProperty.Name.LocalName != SchemaVocab.NavigationProperty)
                            throw new ArgumentException($"The named \"{name}\" Xproperty is not a NavigationProperty.");

                        AddAnnotations(xProperty, mNavigationProperty);
                    }
                }

                AddAnnotations(xEntityType, mEntityType);
            }
        }

        private static void Remove(XElement schema, XElement xRemove)
        {
            foreach (XElement mEntityType in xRemove.Elements(SchemaVocab.EntityType))
            {
                XElement xEntityType = FindXEntityType(mEntityType, schema);

                foreach (XElement mProperty in mEntityType.Elements(SchemaVocab.Property))
                {
                    string name = mProperty.GetAttributeValue(SchemaVocab.Name);
                    XElement? xProperty = FindXProperty(name, xEntityType)
                        ?? throw new ArgumentException($"The named \"{name}\" property is not found in Schema.");

                    if (xProperty.Name.LocalName != SchemaVocab.Property)
                        throw new ArgumentException($"The named \"{name}\" Xproperty is not a Property.");

                    RemoveAnnotations(xProperty, mProperty);
                }

                foreach (XElement mPrincipalProperty in mEntityType.Elements(MetaVocab.PrincipalProperty))
                {
                    string name = mPrincipalProperty.GetAttributeValue(SchemaVocab.Name);
                    XElement? xProperty = FindXProperty(name, xEntityType)
                        ?? throw new ArgumentException($"The named \"{name}\" principalProperty is not found in Schema.");

                    if (xProperty.Name.LocalName != MetaVocab.PrincipalProperty)
                        throw new ArgumentException($"The named \"{name}\" Xproperty is not a PrincipalProperty.");

                    if (mPrincipalProperty.HasElements)
                    {
                        RemoveAnnotations(xProperty, mPrincipalProperty);
                    }
                    else
                    {
                        xProperty.Remove();
                    }
                }

                foreach (XElement mNavigationProperty in mEntityType.Elements(SchemaVocab.NavigationProperty))
                {
                    string name = mNavigationProperty.GetAttributeValue(SchemaVocab.Name);
                    XElement? xProperty = FindXProperty(name, xEntityType)
                        ?? throw new ArgumentException($"The named \"{name}\" NavigationProperty is not found in Schema.");

                    if (xProperty.Name.LocalName != MetaVocab.PrincipalProperty)
                        throw new ArgumentException($"The named \"{name}\" xProperty is not a NavigationProperty.");

                    if (mNavigationProperty.HasElements)
                    {
                        RemoveAnnotations(xProperty, mNavigationProperty);
                    }
                    else
                    {
                        if (mNavigationProperty.GetNullableAttributeValue(MetaVocab.Route) == null)
                            throw new ArgumentException($"Only RouteNavigationProperty can be removed.");

                        xProperty.Remove();
                    }
                }

                RemoveAnnotations(xEntityType, mEntityType);
            }
        }

        private static XElement FindXEntityType(XElement mEntityType, XElement schema)
        {
            string name = mEntityType.GetAttributeValue(SchemaVocab.Name);
            return schema.Elements(SchemaVocab.EntityType).Single(e => e.GetAttributeValue(SchemaVocab.Name) == name);
        }

        /// <param name="mProperty">mProperty, mPrincipalProperty or mNavigationProperty</param>
        /// <param name="xEntityType"></param>
        /// <returns>xProperty, xPrincipalProperty or xNavigationProperty</returns>
        private static XElement? FindXProperty(string name, XElement xEntityType)
        {
            XElement? xProperty = xEntityType.Elements(SchemaVocab.Property).SingleOrDefault(e => e.GetAttributeValue(SchemaVocab.Name) == name);
            if (xProperty != null) return xProperty;

            XElement? xPrincipalProperty = xEntityType.Elements(MetaVocab.PrincipalProperty).SingleOrDefault(e => e.GetAttributeValue(SchemaVocab.Name) == name);
            if (xPrincipalProperty != null) return xPrincipalProperty;

            XElement? xNavigationProperty = xEntityType.Elements(SchemaVocab.NavigationProperty).SingleOrDefault(e => e.GetAttributeValue(SchemaVocab.Name) == name);

            return xNavigationProperty;
        }

        private static void AddAnnotations(XElement xElement, XElement mElement)
        {
            foreach (XElement mAnnotation in mElement.Elements(SchemaVocab.Annotation))
            {
                string type = mAnnotation.GetAttributeValue(SchemaVocab.Type);
                XElement? xAnnotation = FindXAnnotation(type, xElement);
                if (xAnnotation != null) throw new ArgumentException($"The \"{type}\" has already existed.");

                xElement.Add(mAnnotation);
            }
        }

        private static void RemoveAnnotations(XElement xElement, XElement mElement)
        {
            foreach (XElement mAnnotation in mElement.Elements(SchemaVocab.Annotation))
            {
                string type = mAnnotation.GetAttributeValue(SchemaVocab.Type);
                XElement? xAnnotation = FindXAnnotation(type, xElement) ?? throw new ArgumentException($"The \"{type}\" is not found.");

                xAnnotation.Remove();
            }
        }

        private static XElement? FindXAnnotation(string type, XElement xEntityType)
        {
            return xEntityType.Elements(SchemaVocab.Annotation).SingleOrDefault(e => e.GetAttributeValue(SchemaVocab.Type) == type);
        }

    }
}
