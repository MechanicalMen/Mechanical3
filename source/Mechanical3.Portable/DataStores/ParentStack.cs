using System;
using System.Collections.Generic;
using System.Globalization;
using Mechanical3.Core;
using Mechanical3.IO.FileSystems;

namespace Mechanical3.DataStores
{
    /// <summary>
    /// This class is used to keep track of the hierarchy of parents, while exploring a data store.
    /// </summary>
    internal class ParentStack
    {
        #region Item

        /// <summary>
        /// Represents a data store object or array.
        /// </summary>
        internal struct Item
        {
            /// <summary>
            /// Determines whether this data store node is an object, or an array.
            /// </summary>
            internal readonly bool IsObject;

            /// <summary>
            /// The name of the current node, or <c>null</c> if the parent of this node is an array.
            /// </summary>
            internal readonly string Name;

            /// <summary>
            /// The index of the current node, or <c>-1</c> if the parent of this node is an object.
            /// </summary>
            internal readonly int Index;

            /// <summary>
            /// Initializes a new instance of the <see cref="Item"/> struct.
            /// </summary>
            /// <param name="isObject">Determines whether this data store node is an object, or an array.</param>
            /// <param name="name">The name of the current node, or <c>null</c> if the parent of this node is an array.</param>
            /// <param name="index">The index of the current node, or <c>-1</c> if the parent of this node is an object.</param>
            internal Item( bool isObject, string name, int index )
            {
                this.IsObject = isObject;
                this.Name = name;
                this.Index = index;
            }
        }

        #endregion

        #region Private Fields

        private readonly List<Item> parents;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ParentStack"/> class.
        /// </summary>
        internal ParentStack()
        {
            this.parents = new List<Item>();
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Gets a value indicating whether the stack is empty.
        /// </summary>
        /// <value><c>true</c> if the stack is empty; otherwise, <c>false</c>.</value>
        public bool IsRoot
        {
            get { return this.parents.Count == 0; }
        }

        /// <summary>
        /// Gets the last parent pushed into the stack.
        /// </summary>
        /// <value>The current top of the stack.</value>
        public Item DirectParent
        {
            get { return this.parents[this.parents.Count - 1]; }
        }

        /// <summary>
        /// Pushes a data store object parent to the top of the stack.
        /// </summary>
        /// <param name="name">The name of the current node, or <c>null</c> if the parent of this node is an array.</param>
        /// <param name="index">The index of the current node, or <c>-1</c> if the parent of this node is an object.</param>
        public void PushObject( string name = null, int index = -1 )
        {
            this.parents.Add(new Item(true, name, index));
        }

        /// <summary>
        /// Pushes a data store array parent to the top of the stack.
        /// </summary>
        /// <param name="name">The name of the current node, or <c>null</c> if the parent of this node is an array.</param>
        /// <param name="index">The index of the current node, or <c>-1</c> if the parent of this node is an object.</param>
        public void PushArray( string name = null, int index = -1 )
        {
            this.parents.Add(new Item(false, name, index));
        }

        /// <summary>
        /// Removes the top item of the stack.
        /// </summary>
        /// <returns>The previous top of the stack.</returns>
        public Item PopParent()
        {
            var lastIndex = this.parents.Count - 1;
            var result = this.parents[lastIndex];
            this.parents.RemoveAt(lastIndex);
            return result;
        }

        /// <summary>
        /// Gets the path to the current node.
        /// The root node is not part of it, since it has neither name nor index.
        /// </summary>
        /// <param name="currentNodeIsValue"><c>true</c> if the current node is a data store value; otherwise, <c>false</c> for objects and arrays.</param>
        /// <param name="currentName">The name of the current node, or <c>null</c> if the parent of this node is an array.</param>
        /// <param name="currentIndex">The index of the current node, or <c>-1</c> if the parent of this node is an object.</param>
        /// <returns>The path to the current node.</returns>
        public FilePath GetCurrentPath( bool currentNodeIsValue, string currentName = null, int currentIndex = -1 )
        {
            if( this.IsRoot )
                throw new InvalidOperationException("Root nodes have no path!").StoreFileLine();

            // build path from parents
            string name;
            FilePath node;
            FilePath result = null;
            Func<string, string> testName = n => !FilePath.IsValidName(n) ? "<missing_n>" : n;
            Func<int, string> testIndex = i => i < 0 ? "<missing_i>" : i.ToString(CultureInfo.InvariantCulture);

            for( int i = 1; i < this.parents.Count; ++i ) // starting from 1, since the root has neither name nor index
            {
                var curr = this.parents[i];
                var par = this.parents[i - 1];
                name = par.IsObject ? testName(curr.Name) : testIndex(curr.Index);
                node = FilePath.FromDirectoryName(name);
                result = result.NullReference() ? node : result + node;
            }

            // add current node to path
            name = this.DirectParent.IsObject ? testName(currentName) : testIndex(currentIndex);
            node = currentNodeIsValue ? FilePath.FromFileName(name) : FilePath.FromDirectoryName(name);
            result = result.NullReference() ? node : result + node;
            return result;
        }

        #endregion
    }
}
