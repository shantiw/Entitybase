using Shantiw.Data.Meta;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Shantiw.Data.Querying
{
    public partial class QueryBase
    {
        private readonly List<Property> _properties = [];
        public IReadOnlyList<Property> Properties => _properties;

        private readonly List<PrincipalProperty> _principalProperties = [];
        public IReadOnlyList<PrincipalProperty> PrincipalProperties => _principalProperties;

        private readonly List<ComputedProperty> _computedProperties = [];
        public IReadOnlyList<ComputedProperty> ComputedProperties => _computedProperties;

        private readonly List<CalculatedProperty> _calculatedProperties = [];
        public IReadOnlyList<CalculatedProperty> CalculatedProperties => _calculatedProperties;

        public VectNode VectRoot { get; private set; } = VectNode.CreateRoot();

        private readonly Dictionary<string, int> _principalPropertyIds = [];
        public IReadOnlyDictionary<string, int> PrincipalPropertyIds => _principalPropertyIds;

        private int _sequence = 0;
        private int GetSequence()
        {
            ++_sequence;
            return _sequence;
        }

        private void BuildVectTree()
        {
            AggregateProperties();

            //
            foreach (PrincipalProperty prop in PrincipalProperties)
            {
                VectNode.Build(prop.NavigationProperty.Vector, VectRoot, GetSequence, out int lastId);
                if (lastId == -1)
                    _principalPropertyIds.Add(prop.Name, _sequence);
                else
                    _principalPropertyIds.Add(prop.Name, lastId);
            }
        }

        private void AggregateProperties()
        {
            List<string> propNames = [];
            propNames.AddRange(Select.Properties);
            if (Filter != null) propNames.AddRange(Filter.ExpressionObject.PropertyNamePlaceholders.Keys);
            if (OrderBy != null) propNames.AddRange(OrderBy.SortOrders.Select(o => o.Property));

            //
            propNames = [.. propNames.Distinct()];
            foreach (string propName in propNames)
            {
                if (EntityType.ScalarProperties.TryGetValue(propName, out PropertyBase? prop))
                {
                    if (prop is Property p)
                        _properties.Add(p);
                    else if (prop is PrincipalProperty pp)
                        _principalProperties.Add(pp);
                    else if (prop is ComputedProperty cp)
                        _computedProperties.Add(cp);
                    else if (prop is CalculatedProperty calp)
                        _calculatedProperties.Add(calp);
                    else
                        throw new ArgumentException($"Property '{propName}' in ComputedProperty must be a Property or PrincipalProperty.");
                }
                else
                    throw new ArgumentException($"Property '{propName}' not found in entity type '{EntityType.Name}'.");
            }

            //
            propNames = [];
            foreach (ComputedProperty computedProperty in _computedProperties)
            {
                propNames.AddRange(computedProperty.ExpressionObject.PropertyNamePlaceholders.Keys);
            }
            propNames = [.. propNames.Distinct()];
            foreach (string propName in propNames)
            {
                if (EntityType.ScalarProperties.TryGetValue(propName, out PropertyBase? prop))
                {
                    if (prop is Property p)
                    {
                        if (_properties.Any(p => p.Name == propName)) continue;
                        _properties.Add(p);
                    }
                    else if (prop is PrincipalProperty pp)
                    {
                        if (_principalProperties.Any(p => p.Name == propName)) continue;
                        _principalProperties.Add(pp);
                    }

                    Debug.Assert(prop is Property or PrincipalProperty);
                }

                Debug.Assert(EntityType.ScalarProperties.ContainsKey(propName));
            }
        }

    }
}
