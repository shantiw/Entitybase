using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Meta
{
    public partial class PreprocessedClause
    {
        public string Expression { get; private set; }

        public string Clause { get; private set; }

        public IReadOnlyDictionary<string, string> StringPlaceholders { get; private set; }

        public IReadOnlyDictionary<string, string> PropertyPlaceholders { get; private set; }

        public PreprocessedClause(string expression, EntityType entityType)
        {
            Expression = expression;

            //
            string clause = expression;
            Dictionary<string, string> stringPlaceholders = [];
            clause = QuotationPairRegex().Replace(clause, m =>
            {
                if (stringPlaceholders.TryGetValue(m.Value, out string? existingGuid))
                    return existingGuid;

                string guid = GetGuid();
                stringPlaceholders.Add(m.Value, guid);
                return guid;
            });

            if (clause.Contains('\'')) throw new ArgumentException("Unclosed quotation mark after the character string.");
            StringPlaceholders = stringPlaceholders;

            //
            Dictionary<string, string> propertyPlaceholders = [];
            clause = PropertyNameRegex().Replace(clause, m =>
            {
                string propertyName = m.Value;
                if (propertyPlaceholders.TryGetValue(propertyName, out string? existingGuid))
                    return existingGuid;

                if (entityType.ScalarProperties.TryGetValue(propertyName, out PropertyBase? prop))
                {
                    if (prop is Property || prop is PrincipalProperty)
                    {
                        string guid = GetGuid();
                        propertyPlaceholders.Add(propertyName, guid);
                        return guid;
                    }

                    throw new ArgumentException($"The property \"{propertyName}\" must be a {nameof(Property)} or a {nameof(PrincipalProperty)} in the expression.");
                }

                return propertyName;
            });

            Clause = clause;
            PropertyPlaceholders = propertyPlaceholders;
        }

        private static string GetGuid()
        {
            return Guid.NewGuid().ToString("B");
        }

        [GeneratedRegex(@"(')([^']*)\1")]
        private static partial Regex QuotationPairRegex();

        [GeneratedRegex(@"[a-zA-Z_][a-zA-Z0-9_]*")]
        private static partial Regex PropertyNameRegex();

    }
}
