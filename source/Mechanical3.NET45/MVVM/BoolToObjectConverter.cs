using System;
using Mechanical3.Core;

namespace Mechanical3.MVVM
{
    /// <summary>
    /// Chooses between one of two objects.
    /// </summary>
    public class BoolToObjectConverter : SimpleConverterBase<bool, object>
    {
        /// <summary>
        /// Gets or sets the object returned on <c>true</c>.
        /// </summary>
        /// <value>The object returned on <c>true</c>.</value>
        public object True
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the object returned on <c>false</c>.
        /// </summary>
        /// <value>The object returned on <c>false</c>.</value>
        public object False
        {
            get;
            set;
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <returns>A converted value.</returns>
        public override object Convert( bool value )
        {
            return value ? this.True : this.False;
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <returns>A converted value.</returns>
        public override bool ConvertBack( object value )
        {
            if( object.Equals(value, this.True) )
                return true;
            else if( object.Equals(value, this.False) )
                return false;
            else
                throw new ArgumentException().Store(nameof(value), value).Store(nameof(this.True), this.True).Store(nameof(this.False), this.False);
        }
    }
}
