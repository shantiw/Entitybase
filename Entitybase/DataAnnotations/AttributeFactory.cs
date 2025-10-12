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

namespace Shantiw.Data.DataAnnotations
{
    public static partial class AttributeFactory
    {
        public static ValidationAttribute? CreateValidationAttribute(XElement xAnnotation)
        {
            string type = xAnnotation.GetAttributeValue(SchemaVocab.Type);
            return type switch  // AttributeTargets.Property, AllowMultiple = false
            {
                nameof(Base64StringAttribute) => CreateBase64StringAttribute(xAnnotation),
                nameof(CompareAttribute) => CreateCompareAttribute(xAnnotation),
                // nameof(CustomValidationAttribute) => CreateCustomValidationAttribute(xAnnotation),
                nameof(DataTypeAttribute) => CreateDataTypeAttribute(xAnnotation),
                nameof(CreditCardAttribute) => CreateCreditCardAttribute(xAnnotation),
                nameof(EmailAddressAttribute) => CreateEmailAddressAttribute(xAnnotation),
                nameof(EnumDataTypeAttribute) => CreateEnumDataTypeAttribute(xAnnotation),
                nameof(FileExtensionsAttribute) => CreateFileExtensionsAttribute(xAnnotation),
                nameof(PhoneAttribute) => CreatePhoneAttribute(xAnnotation),
                nameof(UrlAttribute) => CreateUrlAttribute(xAnnotation),
                nameof(LengthAttribute) => CreateLengthAttribute(xAnnotation),
                nameof(MaxLengthAttribute) => CreateMaxLengthAttribute(xAnnotation),
                nameof(MinLengthAttribute) => CreateMinLengthAttribute(xAnnotation),
                nameof(RangeAttribute) => CreateRangeAttribute(xAnnotation),
                nameof(RegularExpressionAttribute) => CreateRegularExpressionAttribute(xAnnotation),
                nameof(RequiredAttribute) => CreateRequiredAttribute(xAnnotation),
                nameof(StringLengthAttribute) => CreateStringLengthAttribute(xAnnotation),
                nameof(DataValidationAttribute) => CreateDataValidationAttribute(xAnnotation), // AttributeTargets.Class | AttributeTargets.Property
                _ => null
            };
        }

        public static DataValidationAttribute CreateDataValidationAttribute(XElement xAnnotation)
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(DataValidationAttribute)) throw new ArgumentException(xAnnotation.ToString());

            string booleanExpression = xAnnotation.GetAttributeValue(nameof(DataValidationAttribute.BooleanExpression));
            DataValidationAttribute attribute = new(booleanExpression);
            return attribute;
        }

        public static StorageAttribute? CreateStorageAttribute(XElement xAnnotation)
        {
            string type = xAnnotation.GetAttributeValue(SchemaVocab.Type);
            return type switch  // AttributeTargets.Class, AllowMultiple = false
            {
                nameof(SelectAttribute) => CreateSelectAttribute(xAnnotation),
                nameof(UpdateAttribute) => CreateUpdateAttribute(xAnnotation),
                nameof(DeleteAttribute) => CreateDeleteAttribute(xAnnotation),
                _ => null
            };
        }

        public static SelectAttribute CreateSelectAttribute(XElement xAnnotation)
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(SelectAttribute)) throw new ArgumentException(xAnnotation.ToString());

            string defaultValue = xAnnotation.GetAttributeValue(nameof(SelectAttribute.Default));
            SelectAttribute attribute = new(defaultValue);
            return attribute;
        }

        public static UpdateAttribute CreateUpdateAttribute(XElement xAnnotation)
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(UpdateAttribute)) throw new ArgumentException(xAnnotation.ToString());

            string where = xAnnotation.GetAttributeValue(nameof(UpdateAttribute.Where));
            UpdateAttribute attribute = new(where);
            return attribute;
        }

        public static DeleteAttribute CreateDeleteAttribute(XElement xAnnotation)
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(DeleteAttribute)) throw new ArgumentException(xAnnotation.ToString());

            string where = xAnnotation.GetAttributeValue(nameof(DeleteAttribute.Where));
            DeleteAttribute attribute = new(where);
            return attribute;
        }

    }
}
