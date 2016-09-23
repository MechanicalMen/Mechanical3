using System;

namespace Mechanical3.MVVM
{
    /// <summary>
    /// Negates a <see cref="bool"/> value.
    /// </summary>
    public class NotConverter : SimpleConverterBase<bool, bool>
    {
        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <returns>A converted value.</returns>
        public override bool Convert( bool value )
        {
            return !value;
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <returns>A converted value.</returns>
        public override bool ConvertBack( bool value )
        {
            return !value;
        }
    }
}
