using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace BTS.Common
{
    /// <summary>
    /// Generic Tree collection that can be accessed via a top-to-bottom approach or iterated over in a depth-first manner.
    /// The Tree collection is NOT checked for the introduction of loops, so please take care when using this.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix",
        Justification = "Class is named correctly, and should not end in 'Dictionary'")]
    public sealed class Tree<TKey, TValue> : IDictionary<TKey, Tree<TKey, TValue>>, ICollection<Tree<TKey, TValue>>, IDictionary, ICollection
    {
        /// <summary>
        /// Key for this node
        /// </summary>
        public TKey Key { get; set; }
        /// <summary>
        /// Value of this node
        /// </summary>
        public TValue Value { get; set; }

        /// <summary>
        /// The parent node of this current node, or null if we are the root.
        /// </summary>
        public Tree<TKey, TValue> Parent { get; private set; }
        /// <summary>
        /// Gets a deep count of total number of elements in the tree. To get a shallow count, access Subtrees.Count.
        /// </summary>
        public int Count { get { return Subtrees.Count + Subtrees.Sum(t => t.Value.Count); } }
        /// <summary>
        /// The current depth of this tree node, with 0 being the topmost node (tree root).
        /// </summary>
        public int Depth { get { return (Parent == null) ? 0 : Parent.Depth + 1; } }

        private Dictionary<TKey, Tree<TKey, TValue>> _subtrees = null;
        /// <summary>
        /// All child nodes of this node
        /// </summary>
        public Dictionary<TKey, Tree<TKey, TValue>> Subtrees
        {
            get
            {
                if (_subtrees == null)
                {
                    _subtrees = new Dictionary<TKey, Tree<TKey, TValue>>();
                }

                return _subtrees;
            }
        }

        /// <summary>
        /// Returns false (the tree is not read only)
        /// </summary>
        public bool IsReadOnly { get { return false; } }

        /// <summary>
        /// Provides direct access to a child node, shorthand for Subtrees[key].
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Tree<TKey, TValue> this[TKey key]
        {
            get
            {
                return Subtrees[key];
            }
            set
            {
                Subtrees[key] = value;
            }
        }

        /// <summary>
        /// Creates a new empty root node of the tree
        /// </summary>
        public Tree()
        {
            Key = default;
            Value = default;
        }

        /// <summary>
        /// Creates a new root node of the tree with the specified value
        /// </summary>
        /// <param name="value"></param>
        public Tree(TValue value)
        {
            Key = default;
            Value = value;
        }

        /// <summary>
        /// Creates a new root node of the tree with the specified key and value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public Tree(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        private Tree(Tree<TKey, TValue> parent, TKey key, TValue value)
        {
            Parent = parent;
            Key = key;
            Value = value;
        }

        /// <summary>
        /// Adds a new node under this one with the given key and value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(TKey key, TValue value)
        {
            Subtrees.Add(key, new Tree<TKey, TValue>(this, key, value));
        }

        /// <summary>
        /// Check if any of our immediate children contain the specified key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(TKey key)
        {
            return Subtrees.ContainsKey(key);
        }

        /// <summary>
        /// Removes the node with the given key from our immediate children
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Remove(TKey key)
        {
            return Subtrees.Remove(key);
        }

        /// <summary>
        /// Deletes all children
        /// </summary>
        public void Clear()
        {
            Subtrees.Clear();
        }

        /// <summary>
        /// Enumerates over us and all of our children
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Tree<TKey, TValue>> GetEnumerator()
        {
            return new TreeEnumerator<TKey, TValue>(this);
        }

        /// <summary>
        /// Compares if two tree nodes are the same based on their key and value.
        /// Note that this does NOT consider parent/child relationships in determining equality!
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (!(obj is Tree<TKey, TValue> tree))
            {
                return false;
            }

            return Key.Equals(tree.Key) && Value.Equals(tree.Value);
        }

        /// <summary>
        /// GetHashCode implementation
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #region IDictionary<TKey, TValue> explicit implementations

        void IDictionary<TKey, Tree<TKey, TValue>>.Add(TKey key, Tree<TKey, TValue> value)
        {
            value.Parent = this;
            Subtrees.Add(key, value);
        }

        bool IDictionary<TKey, Tree<TKey, TValue>>.TryGetValue(TKey key, out Tree<TKey, TValue> value)
        {
            return Subtrees.TryGetValue(key, out value);
        }

        ICollection<TKey> IDictionary<TKey, Tree<TKey, TValue>>.Keys
        {
            get { return Subtrees.Keys; }
        }

        ICollection<Tree<TKey, TValue>> IDictionary<TKey, Tree<TKey, TValue>>.Values
        {
            get { return Subtrees.Values; }
        }

        void ICollection<KeyValuePair<TKey, Tree<TKey, TValue>>>.Add(KeyValuePair<TKey, Tree<TKey, TValue>> item)
        {
            item.Value.Parent = this;
            ((ICollection<KeyValuePair<TKey, Tree<TKey, TValue>>>)Subtrees).Add(item);
        }

        bool ICollection<KeyValuePair<TKey, Tree<TKey, TValue>>>.Contains(KeyValuePair<TKey, Tree<TKey, TValue>> item)
        {
            return ((ICollection<KeyValuePair<TKey, Tree<TKey, TValue>>>)Subtrees).Contains(item);
        }

        void ICollection<KeyValuePair<TKey, Tree<TKey, TValue>>>.CopyTo(KeyValuePair<TKey, Tree<TKey, TValue>>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, Tree<TKey, TValue>>>)Subtrees).CopyTo(array, arrayIndex);
        }

        int ICollection<KeyValuePair<TKey, Tree<TKey, TValue>>>.Count
        {
            get { return Subtrees.Count; }
        }

        bool ICollection<KeyValuePair<TKey, Tree<TKey, TValue>>>.Remove(KeyValuePair<TKey, Tree<TKey, TValue>> item)
        {
            return ((ICollection<KeyValuePair<TKey, Tree<TKey, TValue>>>)Subtrees).Remove(item);
        }

        IEnumerator<KeyValuePair<TKey, Tree<TKey, TValue>>> IEnumerable<KeyValuePair<TKey, Tree<TKey, TValue>>>.GetEnumerator()
        {
            return ((ICollection<KeyValuePair<TKey, Tree<TKey, TValue>>>)Subtrees).GetEnumerator();
        }

        #endregion

        #region ICollection<Tree<TKey, TValue>> explicit implementations

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void ICollection<Tree<TKey, TValue>>.Add(Tree<TKey, TValue> item)
        {
            item.Parent = this;
            Subtrees.Add(item.Key, item);
        }

        bool ICollection<Tree<TKey, TValue>>.Contains(Tree<TKey, TValue> item)
        {
            return Subtrees.ContainsValue(item) || Subtrees.Any(t => ((ICollection<Tree<TKey, TValue>>)t.Value).Contains(item));
        }

        void ICollection<Tree<TKey, TValue>>.CopyTo(Tree<TKey, TValue>[] array, int arrayIndex)
        {
            int i = 0;

            foreach (var el in this)
            {
                array[arrayIndex + i] = el;
                i++;
            }
        }

        bool ICollection<Tree<TKey, TValue>>.Remove(Tree<TKey, TValue> item)
        {
            return Subtrees.Remove(item.Key);
        }

        #endregion

        #region IDictionary explicit implementations

        void IDictionary.Add(object key, object value)
        {
            ((IDictionary)Subtrees).Add(key, value);
        }

        bool IDictionary.Contains(object key)
        {
            return ((IDictionary)Subtrees).Contains(key);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return ((IDictionary)Subtrees).GetEnumerator();
        }

        bool IDictionary.IsFixedSize
        {
            get { return false; }
        }

        ICollection IDictionary.Keys
        {
            get { return ((IDictionary)Subtrees).Keys; }
        }

        void IDictionary.Remove(object key)
        {
            ((IDictionary)Subtrees).Remove(key);
        }

        ICollection IDictionary.Values
        {
            get { return ((IDictionary)Subtrees).Values; }
        }

        object IDictionary.this[object key]
        {
            get
            {
                return ((IDictionary)Subtrees)[key];
            }
            set
            {
                ((IDictionary)Subtrees)[key] = value;
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((IDictionary)Subtrees).CopyTo(array, index);
        }

        #endregion

        #region ICollection explicit implementations

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        private readonly object ourLock = new object();

        /// <summary>
        /// Due to the deep nesting of collections, this does NOT guarantee that no other threads will utilize the underlying collections.
        /// </summary>
        object ICollection.SyncRoot
        {
            get { return ourLock; }
        }

        #endregion
    }

    /// <summary>
    /// Represents a depth-first enumerator over the tree
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public sealed class TreeEnumerator<TKey, TValue> : IEnumerator<Tree<TKey, TValue>>, IEnumerator
    {
        private Stack<IEnumerator<KeyValuePair<TKey, Tree<TKey, TValue>>>> Stack { get; set; }
        private bool Init { get; set; }

        internal TreeEnumerator(Tree<TKey, TValue> root)
        {
            Stack = new Stack<IEnumerator<KeyValuePair<TKey, Tree<TKey, TValue>>>>();
            Stack.Push(root.Subtrees.GetEnumerator());
            Init = false;
        }

        /// <summary>
        /// Returns the current node
        /// </summary>
        public Tree<TKey, TValue> Current
        {
            get
            {
                return Stack.Peek().Current.Value;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        /// <summary>
        /// Releases all resources held by the enumerator
        /// </summary>
        public void Dispose()
        {
            while (Stack.Count > 0)
            {
                Stack.Pop().Dispose();
            }
        }

        /// <summary>
        /// Advances the enumerator
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            if (!Init)
            {
                // advance to the first element
                Init = true;
                return Stack.Peek().MoveNext();
            }

            // try to get a subtree of this element
            if (Stack.Peek().Current.Value.Subtrees.Count > 0)
            {
                Stack.Push(Stack.Peek().Current.Value.Subtrees.GetEnumerator());
                Stack.Peek().MoveNext();
                return true;
            }

            // move the current stack item forward; if that's done then pop it and move the parent forward
            // continue until we succeed or unwind the entire stack
            bool success = Stack.Peek().MoveNext();

            while (!success && Stack.Count > 0)
            {
                Stack.Pop();
                success = Stack.Peek().MoveNext();
            }

            return Stack.Count > 0;
        }

        /// <summary>
        /// Reset() is not supported on Tree&lt;TKey, TValue&gt;
        /// </summary>
        public void Reset()
        {
            throw new NotImplementedException();
        }
    }
}
