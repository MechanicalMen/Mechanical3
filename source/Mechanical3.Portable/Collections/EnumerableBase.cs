using System.Collections;
using System.Collections.Generic;

namespace Mechanical3.Collections
{
    /// <summary>
    /// An abstract base class for implementing <see cref="ICollection{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the items in the collection.</typeparam>
    public abstract class EnumerableBase<T> : IEnumerable<T>
    {
        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.</returns>
        public abstract IEnumerator<T> GetEnumerator();

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
