using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Schema
{
    public partial class SchemaGenerator
    {
        protected void SetAnnotations(XElement schema)  // CheckConstraint
        {
            foreach (XElement xEntityType in schema.Elements(SchemaVocab.EntityType))
            {
                // DisplayAttribute
                if (Mapper.IsGeneratingDisplayAttrForEntityTypeByCommnent)
                {
                    XNode? xNode = xEntityType.NodesBeforeSelf().LastOrDefault();
                    if (xNode != null && xNode is XComment)
                    {
                        string displayName = xNode.ToString()[4..^3].Trim();
                        xEntityType.Add(new XElement(SchemaVocab.Annotation,
                            new XAttribute(SchemaVocab.Type, nameof(DisplayAttribute)),
                           new XAttribute(nameof(DisplayAttribute.Name), displayName)));
                    }
                }

                //
                foreach (XElement xProperty in xEntityType.Elements(SchemaVocab.Property))
                {
                    // DisplayAttribute
                    if (Mapper.IsGeneratingDisplayAttrForPropertyByCommnent)
                    {
                        XNode? xNode = xProperty.NodesBeforeSelf().LastOrDefault();
                        if (xNode != null && xNode is XComment)
                        {
                            string displayName = xNode.ToString()[4..^3].Trim();
                            xProperty.Add(new XElement(SchemaVocab.Annotation,
                                new XAttribute(SchemaVocab.Type, nameof(DisplayAttribute)),
                                new XAttribute(nameof(DisplayAttribute.Name), displayName)));
                        }
                    }

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

    }
}
