//---------------------------------------------------------------------
// <copyright file="BidirectionalDictionary.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// @owner       Microsoft
// @backupOwner Microsoft
//---------------------------------------------------------------------
namespace System.Data.Entity.Design.PluralizationServices
{
    /// <summary>
    /// This class provide service for both the singularization and pluralization, it takes the word pairs
    /// in the ctor following the rules that the first one is singular and the second one is plural.
    /// </summary>
    internal class BidirectionalDictionary<TFirst, TSecond> where TFirst : notnull where TSecond : notnull
    {
        internal Dictionary<TFirst, TSecond> FirstToSecondDictionary { get; set; }
        internal Dictionary<TSecond, TFirst> SecondToFirstDictionary { get; set; }

        internal BidirectionalDictionary()
        {
            this.FirstToSecondDictionary = [];
            this.SecondToFirstDictionary = [];
        }

        internal BidirectionalDictionary(Dictionary<TFirst, TSecond> firstToSecondDictionary) : this()
        {
            foreach (var key in firstToSecondDictionary.Keys)
            {
                this.AddValue(key, firstToSecondDictionary[key]);
            }
        }

        internal virtual bool ExistsInFirst(TFirst value)
        {
            if (this.FirstToSecondDictionary.ContainsKey(value))
            {
                return true;
            }
            return false;
        }

        internal virtual bool ExistsInSecond(TSecond value)
        {
            if (this.SecondToFirstDictionary.ContainsKey(value))
            {
                return true;
            }
            return false;
        }

        internal virtual TSecond GetSecondValue(TFirst value)
        {
            if (this.ExistsInFirst(value))
            {
                return this.FirstToSecondDictionary[value];
            }
            else
            {
#pragma warning disable CS8603 // 可能返回 null 引用。
                return default;
#pragma warning restore CS8603 // 可能返回 null 引用。
            }
        }

        internal virtual TFirst GetFirstValue(TSecond value)
        {
            if (this.ExistsInSecond(value))
            {
                return this.SecondToFirstDictionary[value];
            }
            else
            {
#pragma warning disable CS8603 // 可能返回 null 引用。
                return default;
#pragma warning restore CS8603 // 可能返回 null 引用。
            }
        }

        internal void AddValue(TFirst firstValue, TSecond secondValue)
        {
            this.FirstToSecondDictionary.Add(firstValue, secondValue);

            this.SecondToFirstDictionary.TryAdd(secondValue, firstValue);
        }
    }
}
