using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;

namespace BTS.Common
{
    /// <summary>
    /// Dictionary that maintains key insertion order
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [Serializable]
    public sealed class OrderedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDictionary, IOrderedDictionary
    {
        private int version;
        private readonly DataStore data;
        private KeyCollection keys;
        private ValueCollection values;

        /// <summary>
        /// Constructs a new empty OrderedDictionary with a default equality comparer
        /// </summary>
        public OrderedDictionary() : this(EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Constructs an OrderedDictionary populated with the passed-in items and a default equality comparer
        /// </summary>
        /// <param name="items"></param>
        public OrderedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> items) : this(items, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Constructs a new empty OrderedDictionary with the specified equality comparer
        /// </summary>
        /// <param name="comparer"></param>
        public OrderedDictionary(IEqualityComparer<TKey> comparer)
        {
            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            version = 0;
            data = new DataStore(comparer);
            keys = null;
            values = null;
        }

        /// <summary>
        /// Constructs an OrderedDictionary populated with the passed-in items and the specified equality comparer
        /// </summary>
        /// <param name="items"></param>
        /// <param name="comparer"></param>
        public OrderedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> items, IEqualityComparer<TKey> comparer)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            version = 0;
            data = new DataStore(comparer);
            keys = null;
            values = null;

            foreach (var item in items)
            {
                Add(item.Key, item.Value);
            }
        }

        /// <summary>
        /// Gets or sets the value corresponding to the given key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue this[TKey key]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                return data[key].Value;
            }

            set
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                data.AddOrReplace(key, value);
                version++;
            }
        }

        object IDictionary.this[object key]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                if (key is TKey)
                {
                    return data[(TKey)key].Value;
                }

                return null;
            }

            set
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                try
                {
                    var tempKey = (TKey)key;

                    try
                    {
                        data.AddOrReplace(tempKey, (TValue)value);
                        version++;
                    }
                    catch (InvalidCastException)
                    {
                        throw new ArgumentException("variable has wrong type", nameof(value));
                    }
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException("variable has wrong type", nameof(key));
                }
            }
        }

        /// <summary>
        /// Gets or sets the value corresponding to the given index in insertion order
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public TValue this[int index]
        {
            get
            {
                return data[index].Value;
            }

            set
            {
                data[index] = new KeyValuePair<TKey, TValue>(data[index].Key, value);
                version++;
            }
        }

        object IOrderedDictionary.this[int index]
        {
            get
            {
                return data[index].Value;
            }

            set
            {
                try
                {
                    data[index] = new KeyValuePair<TKey, TValue>(data[index].Key, (TValue)value);
                    version++;
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException("variable has wrong type", nameof(value));
                }
            }
        }

        /// <summary>
        /// Retrieves the number of items in the dictionary
        /// </summary>
        public int Count
        {
            get
            {
                return data.Count;
            }
        }

        /// <summary>
        /// Retrieves the collection of all keys in the dictionary
        /// </summary>
        public KeyCollection Keys
        {
            get
            {
                if (keys == null)
                {
                    keys = new KeyCollection(this);
                }

                return keys;
            }
        }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get
            {
                return Keys;
            }
        }

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
        {
            get
            {
                return Keys;
            }
        }

        ICollection IDictionary.Keys
        {
            get
            {
                return Keys;
            }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        bool IDictionary.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        bool IDictionary.IsFixedSize
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Retrieves the values of all items in the dictionary
        /// </summary>
        public ValueCollection Values
        {
            get
            {
                if (values == null)
                {
                    values = new ValueCollection(this);
                }

                return values;
            }
        }

        ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            get
            {
                return Values;
            }
        }

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
        {
            get
            {
                return Values;
            }
        }

        ICollection IDictionary.Values
        {
            get
            {
                return Values;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                return ((ICollection)data).SyncRoot;
            }
        }

        /// <summary>
        /// Adds a new key to the dictionary
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(TKey key, TValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (ContainsKey(key))
            {
                throw new ArgumentException("An item with the same key already exists in the dictionary", nameof(key));
            }

            data.Add(new KeyValuePair<TKey, TValue>(key, value));
            version++;
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            if (item.Key == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (ContainsKey(item.Key))
            {
                throw new ArgumentException("An item with the same key already exists in the dictionary", nameof(item));
            }

            data.Add(item);
            version++;
        }

        void IDictionary.Add(object key, object value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            try
            {
                var tempKey = (TKey)key;

                try
                {
                    Add(tempKey, (TValue)value);
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException("variable has wrong type", nameof(value));
                }
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException("variable has wrong type", nameof(key));
            }
        }

        void Insert(int index, TKey key, TValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (index < 0 || index > data.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "index out of range");
            }

            if (ContainsKey(key))
            {
                throw new ArgumentException("key already exists in dictionary", nameof(key));
            }

            data.Insert(index, new KeyValuePair<TKey, TValue>(key, value));
            version++;
        }

        void IOrderedDictionary.Insert(int index, object key, object value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            try
            {
                var tempKey = (TKey)key;

                try
                {
                    Insert(index, tempKey, (TValue)value);
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException("variable has wrong type", nameof(value));
                }
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException("variable has wrong type", nameof(key));
            }
        }

        /// <summary>
        /// Removes the specified key from the dictionary
        /// </summary>
        /// <param name="key"></param>
        /// <returns>Returns true if the key was found and removed, and false if the key was not found</returns>
        public bool Remove(TKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var removed = data.Remove(key);
            if (removed)
            {
                version++;
            }

            return removed;
        }

        void IDictionary.Remove(object key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (key is TKey)
            {
                Remove((TKey)key);
            }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            bool removed = false;

            if (item.Key == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (((ICollection<KeyValuePair<TKey, TValue>>)this).Contains(item))
            {
                removed = Remove(item.Key);
            }

            if (removed)
            {
                version++;
            }

            return removed;
        }

        /// <summary>
        /// Removes the item at the specified index
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            data.RemoveAt(index);
            version++;
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
        {
            data.Clear();
            version++;
        }

        void IDictionary.Clear()
        {
            data.Clear();
            version++;
        }

        /// <summary>
        /// Checks whether or not the dictionary contains the specified key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(TKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return data.Contains(key);
        }

        /// <summary>
        /// Checks whether or not the dictionary contains the specified value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool ContainsValue(TValue value)
        {
            if (value == null)
            {
                foreach (var kvp in this)
                {
                    if (kvp.Value == null)
                    {
                        return true;
                    }
                }
            }
            else
            {
                var c = EqualityComparer<TValue>.Default;
                foreach (var kvp in this)
                {
                    if (c.Equals(value, kvp.Value))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            if (item.Key == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            return ContainsKey(item.Key) && EqualityComparer<TValue>.Default.Equals(this[item.Key], item.Value);
        }

        bool IDictionary.Contains(object key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (key is TKey)
            {
                return ContainsKey((TKey)key);
            }

            return false;
        }

        /// <summary>
        /// Tries to read the value of the specified key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>True if the key was found (value is populated), false if the key was not found (value is defaulted)</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (!ContainsKey(key))
            {
                value = default;
                return false;
            }

            value = this[key];
            return true;
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            data.CopyTo(array, arrayIndex);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException("multi-rank array not supported", nameof(array));
            }

            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException("array has nonzero lower bound", nameof(array));
            }

            if (index < 0 || index > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (array.Length - index < data.Count)
            {
                throw new ArgumentException("array is not large enough to hold the elements of the dictionary", nameof(array));
            }

            if (array is KeyValuePair<TKey, TValue>[] pairs)
            {
                data.CopyTo(pairs, index);
            }
            else if (array is DictionaryEntry[])
            {
                var arr = array as DictionaryEntry[];
                for (int i = 0; i < data.Count; i++)
                {
                    arr[index + i] = new DictionaryEntry(data[i].Key, data[i].Value);
                }
            }
            else
            {
                if (!(array is object[] arr))
                {
                    throw new ArgumentException("array is of wrong type", nameof(array));
                }

                try
                {
                    for (int i = 0; i < data.Count; i++)
                    {
                        arr[index + i] = new KeyValuePair<TKey, TValue>(data[i].Key, data[i].Value);
                    }
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException("array is of wrong type", nameof(array));
                }
            }
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return new Enumerator(this, RetType.KeyValuePair);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this, RetType.KeyValuePair);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new Enumerator(this, RetType.DictEntry);
        }

        IDictionaryEnumerator IOrderedDictionary.GetEnumerator()
        {
            return new Enumerator(this, RetType.DictEntry);
        }

        /// <summary>
        /// Represents a collection of keys in the dictionary
        /// </summary>
        [Serializable]
        [SuppressMessage("Design", "CA1034:Nested types should not be visible",
            Justification = "Keeping the same design as System.Collections.Generic.Dictionary")]
        public sealed class KeyCollection : ICollection<TKey>, ICollection, IReadOnlyCollection<TKey>
        {
            private readonly OrderedDictionary<TKey, TValue> dictionary;

            /// <summary>
            /// Constructs a new KeyCollection for the specified dictionary
            /// </summary>
            /// <param name="dictionary"></param>
            public KeyCollection(OrderedDictionary<TKey, TValue> dictionary)
            {
                this.dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            }

            /// <summary>
            /// Retrieves the number of keys in this collection
            /// </summary>
            public int Count
            {
                get
                {
                    return dictionary.Count;
                }
            }

            bool ICollection<TKey>.IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            void ICollection<TKey>.Add(TKey item)
            {
                throw new NotSupportedException();
            }

            void ICollection<TKey>.Clear()
            {
                throw new NotSupportedException();
            }

            bool ICollection<TKey>.Contains(TKey item)
            {
                return dictionary.ContainsKey(item);
            }

            bool ICollection<TKey>.Remove(TKey item)
            {
                throw new NotSupportedException();
            }

            IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
            {
                return new Enumerator(dictionary);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new Enumerator(dictionary);
            }

            /// <summary>
            /// Gets an enumerator over the collection
            /// </summary>
            /// <returns></returns>
            public Enumerator GetEnumerator()
            {
                return new Enumerator(dictionary);
            }

            /// <summary>
            /// Copies the collection into an array
            /// </summary>
            /// <param name="array"></param>
            /// <param name="index"></param>
            public void CopyTo(TKey[] array, int index)
            {
                if (array == null)
                {
                    throw new ArgumentNullException(nameof(array));
                }

                if (index < 0 || index > array.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                if (array.Length - index < dictionary.Count)
                {
                    throw new ArgumentException("array is not large enough to hold the elements of the dictionary");
                }

                for (int i = 0; i < dictionary.Count; i++)
                {
                    array[index + i] = dictionary.data[i].Key;
                }
            }

            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null)
                {
                    throw new ArgumentNullException(nameof(array));
                }

                if (array.Rank != 1)
                {
                    throw new ArgumentException("multi-rank array not supported");
                }

                if (array.GetLowerBound(0) != 0)
                {
                    throw new ArgumentException("array has nonzero lower bound");
                }

                if (index < 0 || index > array.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                if (array.Length - index < dictionary.Count)
                {
                    throw new ArgumentException("array is not large enough to hold the elements of the dictionary");
                }

                if (array is TKey[] keys)
                {
                    CopyTo(keys, index);
                }
                else
                {
                    if (!(array is object[] objects))
                    {
                        throw new ArgumentException("invalid array type");
                    }

                    try
                    {
                        for (int i = 0; i < dictionary.Count; i++)
                        {
                            objects[index + i] = dictionary.data[i].Key;
                        }
                    }
                    catch (ArrayTypeMismatchException)
                    {
                        throw new ArgumentException("invalid array type");
                    }
                }
            }

            bool ICollection.IsSynchronized
            {
                get
                {
                    return false;
                }
            }

            object ICollection.SyncRoot
            {
                get
                {
                    return ((ICollection)dictionary).SyncRoot;
                }
            }

            /// <summary>
            /// Represents an enumerator over a key collection
            /// </summary>
            [Serializable]
            public struct Enumerator : IEnumerator<TKey>, IEnumerator
            {
                private readonly OrderedDictionary<TKey, TValue> dictionary;
                private int index;
                private readonly int version;

                internal Enumerator(OrderedDictionary<TKey, TValue> dictionary)
                {
                    this.dictionary = dictionary;
                    version = dictionary.version;
                    index = -1;
                    Current = default;
                }

                /// <summary>
                /// Current key
                /// </summary>
                public TKey Current { get; private set; }

                object IEnumerator.Current
                {
                    get
                    {
                        if (version != dictionary.version)
                        {
                            throw new InvalidOperationException("dictionary has been modified");
                        }

                        return Current;
                    }
                }

                /// <summary>
                /// Advance the enumerator
                /// </summary>
                /// <returns></returns>
                public bool MoveNext()
                {
                    if (version != dictionary.version)
                    {
                        throw new InvalidOperationException("dictionary has been modified");
                    }

                    index++;

                    if ((uint)index < (uint)dictionary.Count)
                    {
                        Current = dictionary.data[index].Key;
                        return true;
                    }

                    index = -2;
                    Current = default;
                    return false;
                }

                /// <summary>
                /// This method does not do anything
                /// </summary>
                public void Dispose()
                {
                    // no-op
                }

                void IEnumerator.Reset()
                {
                    if (version != dictionary.version)
                    {
                        throw new InvalidOperationException("dictionary has been modified");
                    }

                    index = -1;
                    Current = default;
                }
            }
        }

        /// <summary>
        /// Stores a collection of all values in the dictionary
        /// </summary>
        [Serializable]
        [SuppressMessage("Design", "CA1034:Nested types should not be visible",
            Justification = "Keeping the same design as System.Collections.Generic.Dictionary")]
        public sealed class ValueCollection : ICollection<TValue>, ICollection, IReadOnlyCollection<TValue>
        {
            private readonly OrderedDictionary<TKey, TValue> dictionary;

            /// <summary>
            /// Constructs a new ValueCollection for the dictionary
            /// </summary>
            /// <param name="dictionary"></param>
            public ValueCollection(OrderedDictionary<TKey, TValue> dictionary)
            {
                this.dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            }

            /// <summary>
            /// Retrieves the number of items in the collection
            /// </summary>
            public int Count
            {
                get
                {
                    return dictionary.Count;
                }
            }

            bool ICollection<TValue>.IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            void ICollection<TValue>.Add(TValue item)
            {
                throw new NotSupportedException();
            }

            void ICollection<TValue>.Clear()
            {
                throw new NotSupportedException();
            }

            bool ICollection<TValue>.Contains(TValue item)
            {
                return dictionary.ContainsValue(item);
            }

            bool ICollection<TValue>.Remove(TValue item)
            {
                throw new NotSupportedException();
            }

            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
            {
                return new Enumerator(dictionary);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new Enumerator(dictionary);
            }

            /// <summary>
            /// Gets an enumerator over the collection
            /// </summary>
            /// <returns></returns>
            public Enumerator GetEnumerator()
            {
                return new Enumerator(dictionary);
            }

            /// <summary>
            /// Copies the collection to an array
            /// </summary>
            /// <param name="array"></param>
            /// <param name="index"></param>
            public void CopyTo(TValue[] array, int index)
            {
                if (array == null)
                {
                    throw new ArgumentNullException(nameof(array));
                }

                if (index < 0 || index > array.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                if (array.Length - index < dictionary.Count)
                {
                    throw new ArgumentException("array is not large enough to hold the elements of the dictionary");
                }

                for (int i = 0; i < dictionary.Count; i++)
                {
                    array[index + i] = dictionary.data[i].Value;
                }
            }

            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null)
                {
                    throw new ArgumentNullException(nameof(array));
                }

                if (array.Rank != 1)
                {
                    throw new ArgumentException("multi-rank array not supported");
                }

                if (array.GetLowerBound(0) != 0)
                {
                    throw new ArgumentException("array has nonzero lower bound");
                }

                if (index < 0 || index > array.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                if (array.Length - index < dictionary.Count)
                {
                    throw new ArgumentException("array is not large enough to hold the elements of the dictionary");
                }

                if (array is TValue[] values)
                {
                    CopyTo(values, index);
                }
                else
                {
                    if (!(array is object[] objects))
                    {
                        throw new ArgumentException("invalid array type");
                    }

                    try
                    {
                        for (int i = 0; i < dictionary.Count; i++)
                        {
                            objects[index + i] = dictionary.data[i].Value;
                        }
                    }
                    catch (ArrayTypeMismatchException)
                    {
                        throw new ArgumentException("invalid array type");
                    }
                }
            }

            bool ICollection.IsSynchronized
            {
                get
                {
                    return false;
                }
            }

            object ICollection.SyncRoot
            {
                get
                {
                    return ((ICollection)dictionary).SyncRoot;
                }
            }

            /// <summary>
            /// An enumerator over a ValueCollection
            /// </summary>
            [Serializable]
            public struct Enumerator : IEnumerator<TValue>, IEnumerator
            {
                private readonly OrderedDictionary<TKey, TValue> dictionary;
                private int index;
                private readonly int version;

                internal Enumerator(OrderedDictionary<TKey, TValue> dictionary)
                {
                    this.dictionary = dictionary;
                    version = dictionary.version;
                    index = -1;
                    Current = default;
                }

                /// <summary>
                /// Current value
                /// </summary>
                public TValue Current { get; private set; }

                object IEnumerator.Current
                {
                    get
                    {
                        if (version != dictionary.version)
                        {
                            throw new InvalidOperationException("dictionary has been modified");
                        }

                        return Current;
                    }
                }

                /// <summary>
                /// Advance the enumerator
                /// </summary>
                /// <returns></returns>
                public bool MoveNext()
                {
                    if (version != dictionary.version)
                    {
                        throw new InvalidOperationException("dictionary has been modified");
                    }

                    index++;

                    if ((uint)index < (uint)dictionary.Count)
                    {
                        Current = dictionary.data[index].Value;
                        return true;
                    }

                    index = -2;
                    Current = default;
                    return false;
                }

                /// <summary>
                /// This method does nothing
                /// </summary>
                public void Dispose()
                {
                    // no-op
                }

                void IEnumerator.Reset()
                {
                    if (version != dictionary.version)
                    {
                        throw new InvalidOperationException("dictionary has been modified");
                    }

                    index = -1;
                    Current = default;
                }
            }
        }

        /// <summary>
        /// An enumerator over all KeyValuePairs in the dictionary
        /// </summary>
        [Serializable]
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDictionaryEnumerator
        {
            private readonly OrderedDictionary<TKey, TValue> dictionary;
            private readonly int version;
            private int index;
            private KeyValuePair<TKey, TValue> current;
            private readonly RetType retType; // what Current returns

            internal Enumerator(OrderedDictionary<TKey, TValue> dictionary, RetType retType)
            {
                this.dictionary = dictionary;
                this.retType = retType;
                version = dictionary.version;
                index = -1;
                current = new KeyValuePair<TKey, TValue>();
            }

            /// <summary>
            /// Current KeyValuePair
            /// </summary>
            public KeyValuePair<TKey, TValue> Current
            {
                get
                {
                    return current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    if (index < 0)
                    {
                        throw new InvalidOperationException("enumerator out of range");
                    }

                    return retType switch
                    {
                        RetType.DictEntry => new DictionaryEntry(current.Key, current.Value),
                        RetType.KeyValuePair => new KeyValuePair<TKey, TValue>(current.Key, current.Value),
                        _ => throw new InvalidOperationException("unrecognized retType; this is an internal programming bug and should never happen"),
                    };
                }
            }

            DictionaryEntry IDictionaryEnumerator.Entry
            {
                get
                {
                    if (index < 0)
                    {
                        throw new InvalidOperationException("enumerator out of range");
                    }


                    return new DictionaryEntry(current.Key, current.Value);
                }
            }

            object IDictionaryEnumerator.Key
            {
                get
                {
                    if (index < 0)
                    {
                        throw new InvalidOperationException("enumerator out of range");
                    }


                    return current.Key;
                }
            }

            object IDictionaryEnumerator.Value
            {
                get
                {
                    if (index < 0)
                    {
                        throw new InvalidOperationException("enumerator out of range");
                    }


                    return current.Value;
                }
            }

            /// <summary>
            /// Advances the enumerator
            /// </summary>
            /// <returns></returns>
            public bool MoveNext()
            {
                if (version != dictionary.version)
                {
                    throw new InvalidOperationException("dictionary has been modified");
                }

                index++;

                if ((uint)index < (uint)dictionary.Count)
                {
                    current = dictionary.data[index];
                    return true;
                }

                index = -2;
                current = new KeyValuePair<TKey, TValue>();
                return false;
            }

            /// <summary>
            /// This method does nothing
            /// </summary>
            public void Dispose()
            {
                // no-op
            }

            void IEnumerator.Reset()
            {
                if (version != dictionary.version)
                {
                    throw new InvalidOperationException("dictionary has been modified");
                }

                index = -1;
                current = new KeyValuePair<TKey, TValue>();
            }
        }

        [Serializable]
        internal enum RetType
        {
            DictEntry,
            KeyValuePair
        }

        [Serializable]
        private sealed class DataStore : KeyedCollection<TKey, KeyValuePair<TKey, TValue>>
        {
            internal DataStore(IEqualityComparer<TKey> comparer) : base(comparer) {}

            protected override TKey GetKeyForItem(KeyValuePair<TKey, TValue> item)
            {
                return item.Key;
            }

            internal void AddOrReplace(TKey key, TValue value)
            {
                for (int i = 0; i < Count; i++)
                {
                    if (Comparer.Equals(key, Items[i].Key))
                    {
                        SetItem(i, new KeyValuePair<TKey, TValue>(key, value));
                        return;
                    }
                }

                Add(new KeyValuePair<TKey, TValue>(key, value));
            }
        }
    }
}
