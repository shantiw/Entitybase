using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Shantiw.Data.Schema
{
    public partial class SchemaGenerator
    {
        protected void SetAnnotations(XElement schema)  // CheckConstraint
        {
            foreach (XElement xEntityType in schema.Elements(SchemaVocab.EntityType))
            {
                AddAttrsByComment(xEntityType, Mapper.EntityTypeCommnentPolicy);

                //
                foreach (XElement xProperty in xEntityType.Elements(SchemaVocab.Property))
                {
                    AddAttrsByComment(xProperty, Mapper.PropertyCommnentPolicy);

                    //
                    string nullable = xProperty.GetAttributeValue(SchemaVocab.Nullable);
                    if (!bool.Parse(nullable))
                    {
                        xProperty.Add(new XElement(SchemaVocab.Annotation,
                            new XAttribute(SchemaVocab.Type, nameof(RequiredAttribute))));
                        // new XAttribute("AllowEmptyStrings", false)));
                    }

                    //
                    string? maxLength = xProperty.GetNullableAttributeValue(SchemaVocab.MaxLength);
                    if (maxLength != null)
                    {
                        long length = long.Parse(maxLength);
                        if (length > 0 && length <= int.MaxValue)
                        {
                            xProperty.Add(new XElement(SchemaVocab.Annotation,
                                new XAttribute(SchemaVocab.Type, nameof(MaxLengthAttribute)),
                                new XAttribute(nameof(MaxLengthAttribute.Length), length)));
                        }
                    }
                }
            }
        }

        private void AddAttrsByComment(XElement xElement, CommnentPolicy commnentPolicy)
        {
            XNode? xNode = xElement.NodesBeforeSelf().LastOrDefault();
            if (xNode != null && xNode is XComment)
            {
                string content = xNode.ToString()[4..^3].Trim();
                if (commnentPolicy.HasFlag(CommnentPolicy.DisplayName))
                {
                    xElement.Add(new XElement(SchemaVocab.Annotation),
                        new XAttribute(SchemaVocab.Type, nameof(DisplayAttribute)),
                        new XAttribute(nameof(DisplayAttribute.Name), content));
                }
                if (commnentPolicy.HasFlag(CommnentPolicy.Description))
                {
                    xElement.Add(new XElement(SchemaVocab.Annotation),
                         new XAttribute(SchemaVocab.Type, nameof(DescriptionAttribute)),
                         new XAttribute(nameof(DescriptionAttribute.Description), content));
                }
            }
        }

    }
}
