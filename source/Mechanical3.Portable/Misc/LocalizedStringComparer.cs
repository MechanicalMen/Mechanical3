using System;
using System.Collections.Generic;
using System.Globalization;
using Mechanical3.Core;

namespace Mechanical3.Misc
{
    /// <summary>
    /// Compares strings using the specified culture and options.
    /// </summary>
    public class LocalizedStringComparer : IComparer<string>
    {
        //// NOTE: We do not implement IEqualityComparer<string>, because there is no correct way
        ////       to implement IEqualityComparer<string>.GetHashCode(string) using CompareInfo.
        ////       (... in the current portable library; it is possible in .NET 4.6, and maybe earlier).

        #region Internal Fields

#pragma warning disable SA1600, SA1401 // Elements must be documented, Field must be private
        internal readonly CompareInfo CompareInfo;
        internal readonly CompareOptions CompareOptions;
#pragma warning restore SA1600, SA1401

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizedStringComparer"/> class.
        /// </summary>
        /// <param name="info">The <see cref="System.Globalization.CompareInfo"/> to use for comparison. You can get this from <see cref="CultureInfo.CompareInfo"/>.</param>
        /// <param name="options">The <see cref="System.Globalization.CompareOptions"/> to use for comparison.</param>
        public LocalizedStringComparer( CompareInfo info, CompareOptions options )
        {
            if( info.NullReference() )
                throw new ArgumentNullException(nameof(info)).StoreFileLine();

            this.CompareInfo = info;
            this.CompareOptions = options;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizedStringComparer"/> class.
        /// </summary>
        /// <param name="info">The <see cref="CultureInfo"/> to get the <see cref="System.Globalization.CompareInfo"/> from.</param>
        /// <param name="options">The <see cref="System.Globalization.CompareOptions"/> to use for comparison.</param>
        public LocalizedStringComparer( CultureInfo info, CompareOptions options )
            : this(info?.CompareInfo, options)
        {
        }

        #endregion

        #region IComparer

        /// <summary>
        /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns>A signed integer that indicates the relative values of <paramref name="x"/> and <paramref name="y"/>.</returns>
        public int Compare( string x, string y )
        {
            return this.CompareInfo.Compare(x, y, this.CompareOptions);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Determines whether the specified objects are equal.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns><c>true</c> if the specified objects are equal; otherwise, <c>false</c>.</returns>
        public bool Equals( string x, string y )
        {
            return this.Compare(x, y) == 0;
        }

        #endregion
    }
}
