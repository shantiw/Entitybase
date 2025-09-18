using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Schema
{
    public partial class SchemaGenerator
    {
        protected void SetRelationships(XElement schema)
        {
            foreach (XElement xTable in DatabaseSchema.Elements(DbSchemaVocab.Table))
            {
                string tableName = xTable.GetAttributeValue(DbSchemaVocab.Name);
                XElement xEntityType = GetEntityType(tableName, schema);

                foreach (XElement xForeignKey in xTable.Elements(DbSchemaVocab.ForeignKey))
                {
                    SetRelationship(xForeignKey, xEntityType, schema);
                }
            }

            // many to many
            IEnumerable<XElement> xEntityTypes = [.. schema.Elements(SchemaVocab.EntityType).Where(e => e.Elements(SchemaVocab.NavigationProperty).Count() == 2)];
            foreach (XElement xEntityType in xEntityTypes)
            {
                if (!IsJoinEntityType(xEntityType, schema)) continue;

                xEntityType.Remove();

                XElement[] xNavigationProperties = [.. xEntityType.Elements(SchemaVocab.NavigationProperty)];
                xNavigationProperties[0].Remove();
                xNavigationProperties[1].Remove();

                //
                XElement xAssociation1 = GetAssociation(xNavigationProperties[0], schema);
                XElement xAssociation2 = GetAssociation(xNavigationProperties[1], schema);
                xAssociation1.Remove();
                xAssociation2.Remove();

                string associationName1 = xAssociation1.GetAttributeValue(SchemaVocab.Name);
                string associationName2 = xAssociation2.GetAttributeValue(SchemaVocab.Name);

                XElement xEnd1 = GetRelatedEnd(xAssociation1);
                XElement xEnd2 = GetRelatedEnd(xAssociation2);
                xEnd1.Remove();
                xEnd2.Remove();

                xEnd1.SetAttributeValue(nameof(Multiplicity), Multiplicity.Many);
                xEnd2.SetAttributeValue(nameof(Multiplicity), Multiplicity.Many);
                string role1 = xEnd1.GetAttributeValue(SchemaVocab.Role);
                string role2 = xEnd2.GetAttributeValue(SchemaVocab.Role);

                //
                XElement xReferentialConstraint1 = xAssociation1.Elements(SchemaVocab.ReferentialConstraint).Single();
                xReferentialConstraint1.SetAttributeValue(SchemaVocab.Name, xAssociation1.GetAttributeValue(SchemaVocab.Name));
                XElement xReferentialConstraint2 = xAssociation2.Elements(SchemaVocab.ReferentialConstraint).Single();
                xReferentialConstraint2.SetAttributeValue(SchemaVocab.Name, xAssociation2.GetAttributeValue(SchemaVocab.Name));
                xReferentialConstraint1.Remove();
                xReferentialConstraint2.Remove();

                if (role1 == role2)
                {
                    role2 += 1;
                    xEnd2.SetAttributeValue(SchemaVocab.Role, role2);
                    xReferentialConstraint2.Elements().Single(e => e.GetAttributeValue(SchemaVocab.Role) == role1)
                        .SetAttributeValue(SchemaVocab.Role, role2);
                }

                string associationName = xEntityType.GetAttributeValue(SchemaVocab.Name);
                XElement xAssociation = new(SchemaVocab.Association,
                    new XAttribute(SchemaVocab.Name, associationName),
                    xEnd1, xEnd2, xReferentialConstraint1, xReferentialConstraint2, xEntityType);

                Debug.Assert(xReferentialConstraint1.Elements(SchemaVocab.Dependent).Single().GetAttributeValue(SchemaVocab.Role)
                    == xReferentialConstraint2.Elements(SchemaVocab.Dependent).Single().GetAttributeValue(SchemaVocab.Role));

                schema.Add(xAssociation);

                //
                XElement xRelatedEntityType1 = schema.Elements(SchemaVocab.EntityType).Single(e => e.GetAttributeValue(SchemaVocab.Name) == xEnd1.GetAttributeValue(SchemaVocab.Type));
                XElement xRelatedEntityType2 = schema.Elements(SchemaVocab.EntityType).Single(e => e.GetAttributeValue(SchemaVocab.Name) == xEnd2.GetAttributeValue(SchemaVocab.Type));
                XElement xRelatedNavigationProperty1 = xRelatedEntityType1.Elements(SchemaVocab.NavigationProperty).Single(p => p.GetAttributeValue(SchemaVocab.Relationship) == associationName1);
                XElement xRelatedNavigationProperty2 = xRelatedEntityType2.Elements(SchemaVocab.NavigationProperty).Single(p => p.GetAttributeValue(SchemaVocab.Relationship) == associationName2);

                //
                string relatedEntitySetName1 = xRelatedEntityType1.GetAttributeValue(SchemaVocab.EntitySetName);
                string relatedEntitySetName2 = xRelatedEntityType2.GetAttributeValue(SchemaVocab.EntitySetName);
                relatedEntitySetName1 = GenerateNavigationPropertyName(relatedEntitySetName1, xRelatedEntityType1);
                relatedEntitySetName1 = GenerateNavigationPropertyName(relatedEntitySetName1, xRelatedEntityType2);
                if (relatedEntitySetName1 == relatedEntitySetName2)
                    relatedEntitySetName2 += 1;

                //
                xRelatedNavigationProperty1.SetAttributeValue(SchemaVocab.Name, relatedEntitySetName2);
                xRelatedNavigationProperty2.SetAttributeValue(SchemaVocab.Name, relatedEntitySetName1);

                xRelatedNavigationProperty1.SetAttributeValue(SchemaVocab.Relationship, associationName);
                xRelatedNavigationProperty2.SetAttributeValue(SchemaVocab.Relationship, associationName);

                xRelatedNavigationProperty1.SetAttributeValue(SchemaVocab.FromRole, role1);
                xRelatedNavigationProperty2.SetAttributeValue(SchemaVocab.FromRole, role2);

                xRelatedNavigationProperty1.SetAttributeValue(SchemaVocab.ToRole, role2);
                xRelatedNavigationProperty2.SetAttributeValue(SchemaVocab.ToRole, role1);
            }
        }

        private static XElement GetRelatedEnd(XElement xAssociation)
        {
            XElement? xEnd = xAssociation.Elements(SchemaVocab.End).FirstOrDefault(e => e.GetAttributeValue(nameof(Multiplicity)) == Multiplicity.One);
            xEnd ??= xAssociation.Elements(SchemaVocab.End).Single(e => e.GetAttributeValue(nameof(Multiplicity)) == Multiplicity.ZeroOne);

            return xEnd;
        }

        protected static bool IsJoinEntityType(XElement xEntityType, XElement schema)  // many to many
        {
            IEnumerable<XElement> xNavigationProperties = xEntityType.Elements(SchemaVocab.NavigationProperty);
            XElement xFirst = xNavigationProperties.First();
            XElement xLast = xNavigationProperties.Last();

            IEnumerable<string>? firstNames = GetManyEndPropertyRefNamesByFromRole(xFirst, schema);
            if (firstNames == null) return false;

            IEnumerable<string>? lastNames = GetManyEndPropertyRefNamesByFromRole(xLast, schema);
            if (lastNames == null) return false;

            if (xEntityType.Elements(SchemaVocab.Property).Count() != firstNames.Count() + lastNames.Count())
                return false;

            List<string> names = [.. firstNames, .. lastNames];
            foreach (string name in xEntityType.Elements(SchemaVocab.Property).Select(p => p.GetAttributeValue(SchemaVocab.Name)))
                if (!names.Contains(name)) return false;

            return true;
        }

        private static IEnumerable<string>? GetManyEndPropertyRefNamesByFromRole(XElement xNavigationProperty, XElement schema) // called by IsJoinEntityType only
        {
            string fromRole = xNavigationProperty.GetAttributeValue(SchemaVocab.FromRole);
            XElement xAssociation = GetAssociation(xNavigationProperty, schema);

            IEnumerable<XElement> xEnds = xAssociation.Elements(SchemaVocab.End);
            XElement xFirst = xEnds.First();
            XElement xLast = xEnds.Last();

            XElement xEnd;
            XElement xRelatedEnd;
            if (xLast.GetAttributeValue(SchemaVocab.Role) == fromRole)
            {
                xEnd = xLast;
                xRelatedEnd = xFirst;
            }
            else if (xFirst.GetAttributeValue(SchemaVocab.Role) == fromRole)
            {
                xEnd = xFirst;
                xRelatedEnd = xLast;
            }
            else
                return null;

            if (xRelatedEnd.GetAttributeValue(nameof(Multiplicity)) == Multiplicity.Many)
                return null;

            if (xEnd.GetAttributeValue(nameof(Multiplicity)) == Multiplicity.Many)
            {
                XElement? xReferentialConstraint = xAssociation.Element(SchemaVocab.ReferentialConstraint);
                if (xReferentialConstraint != null)
                {
                    XElement? xDependent = xReferentialConstraint.Element(SchemaVocab.Dependent);
                    if (xDependent != null)
                    {
                        Debug.Assert(xDependent.GetAttributeValue(SchemaVocab.Role) == fromRole);

                        return xDependent.Elements(SchemaVocab.PropertyRef).Select(p => p.GetAttributeValue(SchemaVocab.Name));
                    }
                }
            }

            return null;
        }

        protected static XElement GetAssociation(XElement xNavigationProperty, XElement schema)
        {
            string relationship = xNavigationProperty.GetAttributeValue(SchemaVocab.Relationship);
            return schema.Elements(SchemaVocab.Association).Single(a => a.GetAttributeValue(SchemaVocab.Name) == relationship);
        }

        protected static void SetRelationship(XElement xForeignKey, XElement xEntityType, XElement schema)
        {
            string relatedTableName = xForeignKey.GetAttributeValue(DbSchemaVocab.RelatedTable);
            XElement xRelatedEntityType = GetEntityType(relatedTableName, schema);
            XElement xAssociation = CreateAssociation(xForeignKey, xEntityType, xRelatedEntityType);

            //
            XElement xRelatedEnd = xAssociation.Elements(SchemaVocab.End).First();
            XElement xEnd = xAssociation.Elements(SchemaVocab.End).Last();
            string relationship = xAssociation.GetAttributeValue(SchemaVocab.Name);
            string relatedRole = xRelatedEnd.GetAttributeValue(SchemaVocab.Role);
            string role = xEnd.GetAttributeValue(SchemaVocab.Role);

            //
            string navigationPropertyName = xRelatedEntityType.GetAttributeValue(SchemaVocab.Name);
            XElement xNavigationProperty = new(SchemaVocab.NavigationProperty,
                new XAttribute(SchemaVocab.Name, GenerateNavigationPropertyName(navigationPropertyName, xEntityType)),
                new XAttribute(SchemaVocab.Relationship, relationship),
                new XAttribute(SchemaVocab.FromRole, role),
                new XAttribute(SchemaVocab.ToRole, relatedRole));

            //
            string relatedNavigationPropertyName;
            if (xEnd.GetAttributeValue(nameof(Multiplicity)) == Multiplicity.Many)
                relatedNavigationPropertyName = xEntityType.GetAttributeValue(SchemaVocab.EntitySetName);
            else
                relatedNavigationPropertyName = xEntityType.GetAttributeValue(SchemaVocab.Name);

            XElement xRelatedNavigationProperty = new(SchemaVocab.NavigationProperty,
                new XAttribute(SchemaVocab.Name, GenerateNavigationPropertyName(relatedNavigationPropertyName, xEntityType)),
                new XAttribute(SchemaVocab.Relationship, relationship),
                new XAttribute(SchemaVocab.FromRole, relatedRole),
                new XAttribute(SchemaVocab.ToRole, role));

            //
            xRelatedEntityType.Add(xRelatedNavigationProperty);
            xEntityType.Add(xNavigationProperty);

            //
            schema.Add(xAssociation);
        }

        protected static string GenerateNavigationPropertyName(string initName, XElement xEntityType)
        {
            IEnumerable<string> propertyNames = xEntityType.Elements(SchemaVocab.Property).Select(p => p.GetAttributeValue(SchemaVocab.Name));
            IEnumerable<string> navigationPropertyNames = xEntityType.Elements(SchemaVocab.NavigationProperty).Select(p => p.GetAttributeValue(SchemaVocab.Name));

            string name = initName;
            int index = 1;
            while (propertyNames.Contains(name) || navigationPropertyNames.Contains(name))
            {
                name = initName + index;
                index++;
            }

            return name;
        }

        protected static XElement CreateAssociation(XElement xForeignKey, XElement xEntityType, XElement xRelatedEntityType)
        {
            string foreignKeyName = xForeignKey.GetAttributeValue(DbSchemaVocab.Name);
            XElement xAssociation = new(SchemaVocab.Association,
                new XAttribute(SchemaVocab.Name, foreignKeyName));

            //
            string relatedEntityTypeName = xRelatedEntityType.GetAttributeValue(SchemaVocab.Name);
            string relatedRole = relatedEntityTypeName;
            XElement xRelatedEnd = new(SchemaVocab.End,
                new XAttribute(SchemaVocab.Role, relatedRole),
                new XAttribute(SchemaVocab.Type, relatedEntityTypeName));

            xAssociation.Add(xRelatedEnd);

            //
            string entityTypeName = xEntityType.GetAttributeValue(SchemaVocab.Name);
            string role = entityTypeName;
            if (role == relatedRole) role += 1;
            XElement xEnd = new(SchemaVocab.End,
                new XAttribute(SchemaVocab.Role, role),
                new XAttribute(SchemaVocab.Type, entityTypeName));

            xAssociation.Add(xEnd);

            //     
            XElement xReferentialConstraint = new(SchemaVocab.ReferentialConstraint);

            //
            XElement xPrincipal = new(SchemaVocab.Principal,
                new XAttribute(SchemaVocab.Role, relatedRole));
            xReferentialConstraint.Add(xPrincipal);

            //
            XElement xDependent = new(SchemaVocab.Dependent,
                new XAttribute(SchemaVocab.Role, role));
            xReferentialConstraint.Add(xDependent);

            xAssociation.Add(xReferentialConstraint);

            //
            int order = 1;
            bool anyNullableRelatedProperty = false;
            bool anyNullableProperty = false;
            List<string> propertyNames = [];
            foreach (XElement xColumnRef in xForeignKey.Elements(DbSchemaVocab.ColumnRef))
            {
                string relatedColumnName = xColumnRef.GetAttributeValue(DbSchemaVocab.RelatedColumn);
                XElement xRelatedProperty = GetProperty(relatedColumnName, xRelatedEntityType);
                string relatedPropertyName = xRelatedProperty.GetAttributeValue(SchemaVocab.Name);

                xPrincipal.Add(new XElement(SchemaVocab.PropertyRef,
                    new XAttribute(SchemaVocab.Name, relatedPropertyName),
                    new XAttribute(SchemaVocab.Order, order)));
                if (bool.Parse(xRelatedProperty.GetAttributeValue(SchemaVocab.Nullable))) anyNullableRelatedProperty = true;

                string columnName = xColumnRef.GetAttributeValue(DbSchemaVocab.Name);
                XElement xProperty = GetProperty(columnName, xEntityType);
                string propertyName = xProperty.GetAttributeValue(SchemaVocab.Name);

                propertyNames.Add(propertyName);

                xDependent.Add(new XElement(SchemaVocab.PropertyRef,
                    new XAttribute(SchemaVocab.Name, propertyName),
                    new XAttribute(SchemaVocab.Order, order)));
                if (bool.Parse(xProperty.GetAttributeValue(SchemaVocab.Nullable))) anyNullableProperty = true;

                order++;
            }

            //
            if (xPrincipal.Elements(SchemaVocab.PropertyRef).Count() == 1) // xDependent.Elements(SchemaVocab.PropertyRef).Count() == 1
            {
                xPrincipal.Elements(SchemaVocab.PropertyRef).First().SetAttributeValue(SchemaVocab.Order, null);
                xDependent.Elements(SchemaVocab.PropertyRef).First().SetAttributeValue(SchemaVocab.Order, null);
            }

            xRelatedEnd.SetAttributeValue(nameof(Multiplicity), anyNullableProperty ? Multiplicity.ZeroOne : Multiplicity.One);

            //
            string multiplicity;
            if (IsKey(propertyNames, xEntityType) || FindUnique(propertyNames, xEntityType) != null)
            {
                multiplicity = anyNullableRelatedProperty ? Multiplicity.ZeroOne : Multiplicity.One;
            }
            else
            {
                multiplicity = Multiplicity.Many;
            }

            xEnd.SetAttributeValue(nameof(Multiplicity), multiplicity);

            return xAssociation;
        }

    }
}
