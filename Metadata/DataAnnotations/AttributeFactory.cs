using Shantiw.Data.Schema;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Shantiw.Data.DataAnnotations
{
    internal static class AttributeFactory
    {
        public static Attribute? CreatePropertyComponentModelAttribute(XElement xAnnotation)
        {
            string type = xAnnotation.GetAttributeValue(SchemaVocab.Type);
            return type switch  // AttributeTargets.Property, AllowMultiple = false
            {
                nameof(ConcurrencyCheckAttribute) => CreateConcurrencyCheckAttribute(xAnnotation),
                nameof(DisplayAttribute) => CreateDisplayAttribute(xAnnotation),  // AttributeTargets.Class | AttributeTargets.Property
                nameof(EditableAttribute) => CreateEditableAttribute(xAnnotation),
                nameof(TimestampAttribute) => CreateTimestampAttribute(xAnnotation),
                _ => null
            };
        }

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

        #region System.ComponentModel.DataAnnotations

        public static ConcurrencyCheckAttribute CreateConcurrencyCheckAttribute(XElement xAnnotation)
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(ConcurrencyCheckAttribute)) throw new ArgumentException(xAnnotation.ToString());

            ConcurrencyCheckAttribute attribute = new();

            return attribute;
        }

        public static DisplayAttribute CreateDisplayAttribute(XElement xAnnotation)
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(DisplayAttribute)) throw new ArgumentException(xAnnotation.ToString());

            DisplayAttribute attribute = new()
            {
                Name = xAnnotation.GetNullableAttributeValue(nameof(DisplayAttribute.Name))
            };
            return attribute;
        }

        public static EditableAttribute CreateEditableAttribute(XElement xAnnotation)
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(EditableAttribute)) throw new ArgumentException(xAnnotation.ToString());

            bool allowEdit = bool.Parse(xAnnotation.GetAttributeValue(nameof(EditableAttribute.AllowEdit)));
            EditableAttribute attribute = new(allowEdit);

            return attribute;
        }

        /*
        public static KeyAttribute CreateKeyAttribute(XElement xAnnotation)
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(KeyAttribute)) throw new ArgumentException(xAnnotation.ToString());

            KeyAttribute attribute = new();

            return attribute;
        }
        */

        /*
        public static MetadataTypeAttribute CreateMetadataTypeAttribute(XElement xAnnotation)
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(MetadataTypeAttribute)) throw new ArgumentException(xAnnotation.ToString());

            string sMetadataClassType = xAnnotation.GetAttributeValue(nameof(MetadataTypeAttribute.MetadataClassType));
            Type? metadataClassType = Type.GetType(sMetadataClassType) ?? throw new ArgumentNullException(xAnnotation.ToString());
            MetadataTypeAttribute attribute = new(metadataClassType);

            return attribute;
        }
        */

        /*
        public static ScaffoldColumnAttribute CreateScaffoldColumnAttribute(XElement xAnnotation)
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(ScaffoldColumnAttribute)) throw new ArgumentException(xAnnotation.ToString());

            bool scaffold = bool.Parse(xAnnotation.GetAttributeValue(nameof(ScaffoldColumnAttribute.Scaffold)));
            ScaffoldColumnAttribute attribute = new(scaffold);

            return attribute;
        }
        */

        public static TimestampAttribute CreateTimestampAttribute(XElement xAnnotation)
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(TimestampAttribute)) throw new ArgumentException(xAnnotation.ToString());

            TimestampAttribute attribute = new();

            return attribute;
        }

        #region ValidationAttributes

        public static Base64StringAttribute CreateBase64StringAttribute(XElement xAnnotation)
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(Base64StringAttribute)) throw new ArgumentException(xAnnotation.ToString());

            Base64StringAttribute attribute = new();

            return attribute;
        }

        public static CompareAttribute CreateCompareAttribute(XElement xAnnotation)
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(CompareAttribute)) throw new ArgumentException(xAnnotation.ToString());

            string otherProperty = xAnnotation.GetAttributeValue(nameof(CompareAttribute.OtherProperty));
            CompareAttribute attribute = new(otherProperty);

            return attribute;
        }

        /*
        /// <summary>
        /// Writing your own custom ValidationAttribute class that inherits from CustomValidationAttribute instead of using CustomValidationAttribute.
        /// </summary>
        /// <attribute cref="AttributeUsageAttribute">Set AllowMultiple = false true</attribute>
        /// <param name="xAnnotation"></param>
        /// <returns>CustomValidationAttribute</returns>
        /// <exception cref="ArgumentException">The Type of the xAnnotation is not "CustomValidationAttribute".</exception>
        /// <exception cref="ArgumentNullException">The xAnnotation is null or ValidatorType is not found.</exception>
        public static CustomValidationAttribute CreateCustomValidationAttribute(XElement xAnnotation) // AllowMultiple = true
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(CustomValidationAttribute)) throw new ArgumentException(xAnnotation.ToString());

            string sValidatorType = xAnnotation.GetAttributeValue(nameof(CustomValidationAttribute.ValidatorType));
            Type? validatorType = Type.GetType(sValidatorType) ?? throw new ArgumentNullException(xAnnotation.ToString());
            string method = xAnnotation.GetAttributeValue(nameof(CustomValidationAttribute.Method));
            CustomValidationAttribute attribute = new(validatorType, method);

            return attribute;
        }
        */

        public static DataTypeAttribute CreateDataTypeAttribute(XElement xAnnotation)
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(DataTypeAttribute)) throw new ArgumentException(xAnnotation.ToString());

            string sDataType = xAnnotation.GetAttributeValue(nameof(DataTypeAttribute.DataType));
            DataType dataType = (DataType)Enum.Parse(typeof(DataType), sDataType);
            DataTypeAttribute attribute = new(dataType);

            return attribute;
        }

        public static CreditCardAttribute CreateCreditCardAttribute(XElement xAnnotation)
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(CreditCardAttribute)) throw new ArgumentException(xAnnotation.ToString());

            CreditCardAttribute attribute = new();

            return attribute;
        }

        public static EmailAddressAttribute CreateEmailAddressAttribute(XElement xAnnotation)
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(EmailAddressAttribute)) throw new ArgumentException(xAnnotation.ToString());

            EmailAddressAttribute attribute = new();

            return attribute;
        }

        public static EnumDataTypeAttribute CreateEnumDataTypeAttribute(XElement xAnnotation)
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(EnumDataTypeAttribute)) throw new ArgumentException(xAnnotation.ToString());

            string sEnumType = xAnnotation.GetAttributeValue(nameof(EnumDataTypeAttribute.EnumType));
            Type? enumType = Type.GetType(sEnumType) ?? throw new ArgumentNullException(xAnnotation.ToString());
            EnumDataTypeAttribute attribute = new(enumType);

            return attribute;
        }

        public static FileExtensionsAttribute CreateFileExtensionsAttribute(XElement xAnnotation)
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(FileExtensionsAttribute)) throw new ArgumentException(xAnnotation.ToString());

            FileExtensionsAttribute attribute = new();

            return attribute;
        }

        public static PhoneAttribute CreatePhoneAttribute(XElement xAnnotation)
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(PhoneAttribute)) throw new ArgumentException(xAnnotation.ToString());

            PhoneAttribute attribute = new();

            return attribute;
        }

        public static UrlAttribute CreateUrlAttribute(XElement xAnnotation)
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(UrlAttribute)) throw new ArgumentException(xAnnotation.ToString());

            UrlAttribute attribute = new();

            return attribute;
        }

        public static LengthAttribute CreateLengthAttribute(XElement xAnnotation)
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(LengthAttribute)) throw new ArgumentException(xAnnotation.ToString());

            int minimumLength = int.Parse(xAnnotation.GetAttributeValue(nameof(LengthAttribute.MinimumLength)));
            int maximumLength = int.Parse(xAnnotation.GetAttributeValue(nameof(LengthAttribute.MaximumLength)));
            LengthAttribute attribute = new(minimumLength, maximumLength);

            return attribute;
        }

        public static MaxLengthAttribute CreateMaxLengthAttribute(XElement xAnnotation)
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(MaxLengthAttribute)) throw new ArgumentException(xAnnotation.ToString());

            int length = int.Parse(xAnnotation.GetAttributeValue(nameof(MaxLengthAttribute.Length)));
            MaxLengthAttribute attribute = new(length);

            return attribute;
        }

        public static MinLengthAttribute CreateMinLengthAttribute(XElement xAnnotation)
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(MinLengthAttribute)) throw new ArgumentException(xAnnotation.ToString());

            int length = int.Parse(xAnnotation.GetAttributeValue(nameof(MinLengthAttribute.Length)));
            MinLengthAttribute attribute = new(length);

            return attribute;
        }

        public static RangeAttribute CreateRangeAttribute(XElement xAnnotation)
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(RangeAttribute)) throw new ArgumentException(xAnnotation.ToString());

            double minimum = double.Parse(xAnnotation.GetAttributeValue(nameof(RangeAttribute.Minimum)));
            double maximum = double.Parse(xAnnotation.GetAttributeValue(nameof(RangeAttribute.Minimum)));
            RangeAttribute attribute = new(minimum, maximum);

            return attribute;
        }

        public static RegularExpressionAttribute CreateRegularExpressionAttribute(XElement xAnnotation)
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(RegularExpressionAttribute)) throw new ArgumentException(xAnnotation.ToString());

            string pattern = xAnnotation.GetAttributeValue(nameof(RegularExpressionAttribute.Pattern));
            RegularExpressionAttribute attribute = new(pattern);

            return attribute;
        }

        public static RequiredAttribute CreateRequiredAttribute(XElement xAnnotation)
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(RequiredAttribute)) throw new ArgumentException(xAnnotation.ToString());

            RequiredAttribute attribute = new();
            string? allowEmptyStrings = xAnnotation.GetNullableAttributeValue(nameof(RequiredAttribute.AllowEmptyStrings));
            if (allowEmptyStrings != null)
            {
                attribute.AllowEmptyStrings = bool.Parse(allowEmptyStrings);
            }

            return attribute;
        }

        public static StringLengthAttribute CreateStringLengthAttribute(XElement xAnnotation)
        {
            if (xAnnotation.GetAttributeValue(SchemaVocab.Type) != nameof(StringLengthAttribute)) throw new ArgumentException(xAnnotation.ToString());

            int maximumLength = int.Parse(xAnnotation.GetAttributeValue(nameof(StringLengthAttribute.MaximumLength)));
            StringLengthAttribute attribute = new(maximumLength);

            return attribute;
        }

        #endregion

        #endregion

    }
}
