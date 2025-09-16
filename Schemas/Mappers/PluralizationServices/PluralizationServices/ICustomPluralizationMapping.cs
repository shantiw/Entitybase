//---------------------------------------------------------------------
// <copyright file="ICustomPluralizationMapping.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// @owner       leil
// @backupOwner jeffreed
//---------------------------------------------------------------------
namespace System.Data.Entity.Design.PluralizationServices
{
    public interface ICustomPluralizationMapping
    {
        void AddWord(string singular, string plural);
    }
}
