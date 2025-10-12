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

        private Dictionary<string, NavigationProperty> _navigationProperties = [];
        public IReadOnlyDictionary<string, NavigationProperty> NavigationProperties => _navigationProperties;

        private readonly Dictionary<string, PrincipalProperty> _principalProperties = [];
        public IReadOnlyDictionary<string, PrincipalProperty> PrincipalProperties => _principalProperties;

        private readonly Dictionary<string, ComputedProperty> _computedProperties = [];
        public IReadOnlyDictionary<string, ComputedProperty> ComputedProperties => _computedProperties;

        private readonly Dictionary<string, CalculatedProperty> _calculatedProperties = [];
        public IReadOnlyDictionary<string, CalculatedProperty> CalculatedProperties => _calculatedProperties;

        private readonly Dictionary<string, PropertyBase> _scalarProperties = [];
        public IReadOnlyDictionary<string, PropertyBase> ScalarProperties => _scalarProperties;

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

        public IReadOnlyDictionary<string, Attribute> ComponentModelAttributes { get; private set; }

        public IReadOnlyDictionary<string, ValidationAttribute> ValidationAttributes { get; private set; }

        public IReadOnlyDictionary<string, StorageAttribute> StorageAttributes { get; private set; }

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
                _scalarProperties.Add(property.Name, property);
            }
            Properties = properties;

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
            ValidationAttributes = AttributeUtil.CreateEntityValidations(xEntityType);
            StorageAttributes = AttributeUtil.CreateStorageAttributes(xEntityType);
        }

        internal void BuildRelationshipNavigationProperties()
        {
            foreach (XElement xNavigationProperty in _xEntityType.Elements(nameof(NavigationProperty))
                    .Where(p => p.Attribute(SchemaVocab.Relationship) != null))
            {
                NavigationProperty navigationProperty = new RelationshipNavigationProperty(this, xNavigationProperty);
                _navigationProperties.Add(navigationProperty.Name, navigationProperty);
            }
        }

        internal void BuildPathNavigationProperties()
        {
            foreach (XElement xNavigationProperty in _xEntityType.Elements(nameof(NavigationProperty))
                .Where(p => p.Attribute(nameof(PathNavigationProperty.Path)) != null))
            {
                NavigationProperty navigationProperty = new PathNavigationProperty(this, xNavigationProperty);
                _navigationProperties.Add(navigationProperty.Name, navigationProperty);
            }
        }

        internal void BuildPrincipalProperties()
        {
            foreach (XElement xNavigationProperty in _xEntityType.Elements(nameof(PrincipalProperty)))
            {
                PrincipalProperty principalProperty = new(this, xNavigationProperty);
                _principalProperties.Add(principalProperty.Name, principalProperty);
                _scalarProperties.Add(principalProperty.Name, principalProperty);
            }
        }

        internal void BuildComputedProperties()
        {
            foreach (XElement xComputedProperty in _xEntityType.Elements(nameof(ComputedProperty)))
            {
                ComputedProperty computedProperty = new(this, xComputedProperty);
                _computedProperties.Add(computedProperty.Name, computedProperty);
                _scalarProperties.Add(computedProperty.Name, computedProperty);
            }
        }

        internal void BuildCalculatedProperties()
        {
            foreach (XElement xCalculatedProperty in _xEntityType.Elements(nameof(CalculatedProperty)))
            {
                CalculatedProperty calculatedProperty = new(this, xCalculatedProperty);
                _calculatedProperties.Add(calculatedProperty.Name, calculatedProperty);
                _scalarProperties.Add(calculatedProperty.Name, calculatedProperty);
            }
        }

    }
}
