using System.Data;
using System.Xml.Linq;

namespace System.Xml.Linq
{
    public static class XElementExtensions
    {
        public static string? GetNullableAttributeValue(this XElement element, string attrName)
        {
            XAttribute? attribute = element.Attribute(attrName);
            if (attribute == null) return null;
            return attribute.Value;
        }

        public static string GetAttributeValue(this XElement element, string attrName)
        {
            XAttribute attribute = element.Attribute(attrName) ?? throw CreateAttributeNotFoundException(attrName, element);
            return attribute.Value;
        }

        private static NoNullAllowedException CreateAttributeNotFoundException(string name, XElement element)
        {
            string content = element.ToString();
            int index = content.IndexOf('>');
            if (index != -1)
            {
                content = content[..(index + 1)];
            }

            string message = $"Could not find attribute \"{name}\" in element \"{content}\"";

            return new NoNullAllowedException(message);
        }

    }
}
