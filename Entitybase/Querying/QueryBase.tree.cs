using Shantiw.Data.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Shantiw.Data.Querying
{
    public partial class QueryBase
    {
        private List<string> _properties = [];
        public IReadOnlyList<string> Properties => _properties;

        private List<string> _principalProperties = [];
        public IReadOnlyList<string> PrincipalProperties => _principalProperties;

        private List<string> _computedProperties = [];
        public IReadOnlyList<string> ComputedProperties => _computedProperties;

        private List<string> _calculatedProperties = [];
        public IReadOnlyList<string> CalculatedProperties => _calculatedProperties;

        private void AggregateProperties()
        {
            for (int i = 0; i < Select.Properties.Length; i++)
            {
                string propName = Select.Properties[i];
                if (EntityType.Properties.ContainsKey(propName))
                {
                    _properties.Add(propName);
                }
                else if (EntityType.PrincipalProperties.ContainsKey(propName))
                {
                    _principalProperties.Add(propName);
                }
                else if (EntityType.ComputedProperties.ContainsKey(propName))
                {
                    _computedProperties.Add(propName);
                }
                else if (EntityType.CalculatedProperties.ContainsKey(propName))
                {
                    _calculatedProperties.Add(propName);
                }
                else
                {
                    throw new ArgumentException($"Property '{propName}' does not exist in entity type '{EntityType.Name}'.");
                }
            }
            if (Filter != null)
            {
                _properties.AddRange(Filter.PreprocessedClause.Properties);
                _principalProperties.AddRange(Filter.PreprocessedClause.PrincipalProperties);
            }
            if (OrderBy != null)
            {
                foreach (Order order in OrderBy.Orders)
                {
                    string propName = order.Property;
                    if (EntityType.Properties.ContainsKey(propName))
                    {
                        _properties.Add(propName);
                    }
                    else if (EntityType.PrincipalProperties.ContainsKey(propName))
                    {
                        _principalProperties.Add(propName);
                    }
                    else if (EntityType.ComputedProperties.ContainsKey(propName))
                    {
                        _computedProperties.Add(propName);
                    }
                    else if (EntityType.CalculatedProperties.ContainsKey(propName))
                    {
                        throw new ArgumentException($"A {nameof(CalculatedProperty)} is not supported in OrderBy.");
                    }
                    else
                    {
                        throw new ArgumentException($"Property '{propName}' does not exist in entity type '{EntityType.Name}'.");
                    }
                }
            }

            //
            _properties = [.. _properties.Distinct()];
            _principalProperties = [.. _principalProperties.Distinct()];
            _computedProperties = [.. _computedProperties.Distinct()];
            _calculatedProperties = [.. _calculatedProperties.Distinct()];
        }

        private int _sequence = 0;
        private int GetSequence()
        {
            ++_sequence;
            return _sequence;
        }

        void BuildTree()
        {
          
        }

    }
}
