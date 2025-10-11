using Shantiw.Data.DataAnnotations;
using Shantiw.Data.Schema;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Shantiw.Data.Meta
{
    internal static class AttributeUtil
    {
        internal static string? GetDisplayName(IReadOnlyDictionary<string, Attribute> ComponentModelAttributes)
        {
            if (ComponentModelAttributes.TryGetValue(nameof(DisplayAttribute), out Attribute? attribute))
            {
                DisplayAttribute displayAttribute = (DisplayAttribute)attribute;
                if (displayAttribute.Name != null) return displayAttribute.Name;
            }

            return null;
        }

        internal static IReadOnlyDictionary<string, Attribute> CreateComponentModelAttributes(XElement xElement)
        {
            Dictionary<string, Attribute> attributes = [];
            foreach (XElement xAnnotation in xElement.Elements(SchemaVocab.Annotation))
            {
                string type = xAnnotation.GetAttributeValue(SchemaVocab.Type);
                Attribute? attribute = type switch
                {
                    nameof(DisplayAttribute) => AttributeFactory.CreateDisplayAttribute(xAnnotation), // AttributeTargets.Class | AttributeTargets.Property
                    nameof(DescriptionAttribute) => AttributeFactory.CreateDescriptionAttribute(xAnnotation), // AttributeTargets.All
                    _ => null
                };
                if (attribute == null) continue;

                attributes.Add(type, attribute);
            }

            return attributes;
        }

        internal static IReadOnlyDictionary<string, StorageAttribute> CreateStorageAttributes(XElement xElement)
        {
            Dictionary<string, StorageAttribute> attributes = [];
            foreach (XElement xAnnotation in xElement.Elements(SchemaVocab.Annotation))
            {
                StorageAttribute? attribute = AttributeFactory.CreateStorageAttribute(xAnnotation);
                if (attribute == null) continue;

                attributes.Add(attribute.GetType().Name, attribute);
            }

            return attributes;
        }

        internal static IReadOnlyDictionary<string, Attribute> CreatePropertyDataAnnotations(XElement xElement)
        {
            Dictionary<string, Attribute> attributes = [];
            foreach (XElement xAnnotation in xElement.Elements(SchemaVocab.Annotation))
            {
                Attribute? attribute = CreatePropertyDataAnnotation(xAnnotation);
                if (attribute == null) continue;

                attributes.Add(attribute.GetType().Name, attribute);
            }

            return attributes;
        }

        private static Attribute? CreatePropertyDataAnnotation(XElement xAnnotation)
        {
            string type = xAnnotation.GetAttributeValue(SchemaVocab.Type);
            return type switch  // AttributeTargets.Property, AllowMultiple = false
            {
                nameof(ConcurrencyCheckAttribute) => AttributeFactory.CreateConcurrencyCheckAttribute(xAnnotation),
                nameof(EditableAttribute) => AttributeFactory.CreateEditableAttribute(xAnnotation),
                nameof(TimestampAttribute) => AttributeFactory.CreateTimestampAttribute(xAnnotation),
                _ => null
            };
        }

        internal static IReadOnlyDictionary<string, ValidationAttribute> CreateEntityValidations(XElement xElement)
        {
            Dictionary<string, ValidationAttribute> validationAttributes = [];
            foreach (XElement xAnnotation in xElement.Elements(SchemaVocab.Annotation))
            {
                string type = xAnnotation.GetAttributeValue(SchemaVocab.Type);
                ValidationAttribute? validationAttribute = type switch
                {
                    nameof(DataValidationAttribute) => AttributeFactory.CreateDataValidationAttribute(xAnnotation),
                    _ => null
                };
                if (validationAttribute == null) continue;

                validationAttributes.Add(type, validationAttribute);
            }

            return validationAttributes;
        }

        internal static IReadOnlyDictionary<string, ValidationAttribute> CreatePropertyValidations(XElement xElement)
        {
            Dictionary<string, ValidationAttribute> validationAttributes = [];
            foreach (XElement xAnnotation in xElement.Elements(SchemaVocab.Annotation))
            {
                ValidationAttribute? validationAttribute = AttributeFactory.CreateValidationAttribute(xAnnotation);
                if (validationAttribute == null) continue;

                validationAttributes.Add(validationAttribute.GetType().Name, validationAttribute);
            }

            return validationAttributes;
        }

    }
}
