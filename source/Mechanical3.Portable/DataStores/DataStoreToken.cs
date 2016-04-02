namespace Mechanical3.DataStores
{
    /// <summary>
    /// The data store tokens.
    /// </summary>
    public enum DataStoreToken : byte
    {
        /// <summary>
        /// A value in the data store.
        /// </summary>
        Value,

        /// <summary>
        /// The start of an array. Items in an array do not have names.
        /// </summary>
        ArrayStart,

        /// <summary>
        /// The start of an object. Items in an object have unique names.
        /// </summary>
        ObjectStart,

        /// <summary>
        /// The end of an array or object.
        /// </summary>
        End,
    }
}
