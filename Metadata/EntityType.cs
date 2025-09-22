using Shantiw.Data.DataAnnotations;
using Shantiw.Data.Schema;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Meta
{
    public class EntityType
    {
        public EntityDataModel EntityDataModel { get; private set; }

        public string Name { get; private set; }

        public string EntitySetName { get; private set; }

        public string TableName { get; private set; }

        public IReadOnlyDictionary<string, Property>? Key { get; private set; }

        public IReadOnlyDictionary<string, Property> Properties { get; private set; }

        public IReadOnlyDictionary<string, CalculatedProperty> CalculatedProperties { get; private set; }

        public IReadOnlyDictionary<string, PrincipalProperty> PrincipalProperties { get; private set; } = new Dictionary<string, PrincipalProperty>();

        public IReadOnlyDictionary<string, NavigationProperty> NavigationProperties { get; private set; } = new Dictionary<string, NavigationProperty>();

        private string? _displayName = null;
        public string DisplayName
        {
            get
            {
                if (_displayName == null)
                {
                    string? displayName = AttributeUtil.GetDisplayName(ComponentModelAttributes);
                    _displayName = displayName ?? Name;
                }
                return _displayName;
            }
        }

        public IReadOnlyDictionary<string, Attribute> ComponentModelAttributes { get; private set; } // DisplayAttribute

        public IReadOnlyDictionary<string, ValidationAttribute> ValidationAttributes { get; private set; }

        private readonly XElement _xEntityType;

        internal EntityType(EntityDataModel model, XElement xEntityType)
        {
            _xEntityType = xEntityType;

            EntityDataModel = model;
            Name = xEntityType.GetAttributeValue(SchemaVocab.Name);
            EntitySetName = xEntityType.GetAttributeValue(SchemaVocab.EntitySetName);
            TableName = xEntityType.GetAttributeValue(SchemaVocab.TableName);

            //
            Dictionary<string, Property> properties = [];
            foreach (XElement xProperty in xEntityType.Elements(nameof(Property)))
            {
                Property property = new(this, xProperty);
                properties.Add(property.Name, property);
            }
            Properties = properties;

            //
            Dictionary<string, CalculatedProperty> calculatedProperties = [];
            foreach (XElement xCalculatedProperty in xEntityType.Elements(nameof(CalculatedProperty)))
            {
                CalculatedProperty calculatedProperty = new(this, xCalculatedProperty);
                calculatedProperties.Add(calculatedProperty.Name, calculatedProperty);
            }
            CalculatedProperties = calculatedProperties;

            //
            XElement? xKey = xEntityType.Element(nameof(Key));
            if (xKey != null)
            {
                Dictionary<string, Property> key = [];
                foreach (XElement xPropertyRef in xKey.Elements(SchemaVocab.PropertyRef))
                {
                    string name = xPropertyRef.GetAttributeValue(nameof(Property.Name));
                    key.Add(name, Properties[name]);
                }
                Key = key;
            }

            //
            ComponentModelAttributes = AttributeUtil.CreateComponentModelAttributes(xEntityType);
            ValidationAttributes = AttributeUtil.CreateEntityValidationAttributes(xEntityType);
        }

        internal void BuildRelationshipNavigationProperties()
        {
            Dictionary<string, NavigationProperty> navigationProperties = (Dictionary<string, NavigationProperty>)NavigationProperties;
            foreach (XElement xNavigationProperty in _xEntityType.Elements(nameof(NavigationProperty))
                    .Where(p => p.Attribute(SchemaVocab.Relationship) != null))
            {
                NavigationProperty navigationProperty = new RelationshipNavigationProperty(this, xNavigationProperty);
                navigationProperties.Add(navigationProperty.Name, navigationProperty);
            }
        }

        internal void BuildPrincipalAndRouteNavigationProperties()
        {
            Dictionary<string, PrincipalProperty> principalProperties = (Dictionary<string, PrincipalProperty>)PrincipalProperties;
            foreach (XElement xNavigationProperty in _xEntityType.Elements(nameof(PrincipalProperty)))
            {
                PrincipalProperty principalProperty = new(this, xNavigationProperty);
                principalProperties.Add(principalProperty.Name, principalProperty);
            }

            //
            Dictionary<string, NavigationProperty> navigationProperties = (Dictionary<string, NavigationProperty>)NavigationProperties;
            foreach (XElement xNavigationProperty in _xEntityType.Elements(nameof(NavigationProperty))
                .Where(p => p.Attribute(nameof(RouteNavigationProperty.Route)) != null))
            {
                NavigationProperty navigationProperty = new RouteNavigationProperty(this, xNavigationProperty);
                navigationProperties.Add(navigationProperty.Name, navigationProperty);
            }
        }

    }
}
