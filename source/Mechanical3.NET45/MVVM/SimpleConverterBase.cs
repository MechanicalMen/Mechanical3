using System;
using System.Globalization;
using System.Windows.Data;

namespace Mechanical3.MVVM
{
    /// <summary>
    /// A base class for binding converters.
    /// </summary>
    /// <typeparam name="TSource">The type of the binding source.</typeparam>
    /// <typeparam name="TTarget">The type of the binding target.</typeparam>
    public abstract class SimpleConverterBase<TSource, TTarget> : IValueConverter
    {
        #region IValueConverter
#pragma warning disable SA1600 // elements must be documented
        object IValueConverter.Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            return this.Convert((TSource)value);
        }

        object IValueConverter.ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            return this.ConvertBack((TTarget)value);
        }
#pragma warning restore SA1600
        #endregion

        #region Public Methods

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <returns>A converted value.</returns>
        public abstract TTarget Convert( TSource value );

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <returns>A converted value.</returns>
        public abstract TSource ConvertBack( TTarget value );

        #endregion
    }
}
