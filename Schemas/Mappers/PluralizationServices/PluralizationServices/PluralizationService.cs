//---------------------------------------------------------------------
// <copyright file="PluralizationService.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// @owner       venkatja
// @backupOwner willa
//---------------------------------------------------------------------
using System.Data.Entity.Design.Common;
using System.Globalization;

namespace System.Data.Entity.Design.PluralizationServices
{
    public abstract class PluralizationService
    {
        public CultureInfo Culture { get; protected set; } = new CultureInfo("en-US");

        public abstract bool IsPlural(string word);
        public abstract bool IsSingular(string word);
        public abstract string Pluralize(string word);
        public abstract string Singularize(string word);

        /// <summary>
        /// Factory method for PluralizationService. Only support english pluralization.
        /// Please set the PluralizationService on the System.Data.Entity.Design.EntityModelSchemaGenerator
        /// to extend the service to other locales.
        /// </summary>
        /// <param name="culture">CultureInfo</param>
        /// <returns>PluralizationService</returns>
        public static PluralizationService CreateService(CultureInfo culture)
        {
            EDesignUtil.CheckArgumentNull<CultureInfo>(culture, "culture");

            if (culture.TwoLetterISOLanguageName == "en")
            {
                return new EnglishPluralizationService();
            }
            else
            {
                throw new NotImplementedException(string.Format("Unsupported locale '{0}' for PluralizationServices", culture.DisplayName));
                // Strings.UnsupportedLocaleForPluralizationServices(culture.DisplayName));
            }
        }

        // 
        public static PluralizationService CreateEnglishPluralizationService()
        {
            return new EnglishPluralizationService();
        }
    }
}