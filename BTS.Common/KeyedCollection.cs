using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace BTS.Common
{
    /// <summary>
    /// A key-value store which can be accessed either by string key or int index and serialized into XML.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    [XmlRoot("KeyedCollection"), Serializable]
    public sealed class KeyedCollection<TValue> : NameObjectCollectionBase, ICollection<KeyValuePair<string, TValue>>, IEnumerable<KeyValuePair<string, TValue>>, IXmlSerializable
    {
        /// <summary>
        /// Construct a new empty KeyedCollection
        /// </summary>
        public KeyedCollection() { }

        /// <summary>
        /// Serialization constructor
        /// </summary>
        /// <param name="serializationInfo"></param>
        /// <param name="streamingContext"></param>
        private KeyedCollection(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext) { }

        /// <summary>
        /// Retrieves or sets the value at the given index (in insertion order)
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public TValue this[int index]
        {
            get
            {
                return (TValue)BaseGet(index);
            }
            set
            {
                BaseSet(index, value);
            }
        }

        /// <summary>
        /// Retrieves or sets the value at the given key. Should the key exist multiple times
        /// in the collection, this operates on the first instance of the key found (in insertion order).
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue this[string key]
        {
            get
            {
                return (TValue)BaseGet(key);
            }
            set
            {
                BaseSet(key, value);
            }
        }

        /// <summary>
        /// Whether or not this collection is read only
        /// </summary>
        public new bool IsReadOnly
        {
            get
            {
                return base.IsReadOnly;
            }
        }

        /// <summary>
        /// Adds a key to the end of the collection
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(string key, TValue value)
        {
            BaseAdd(key, value);
        }

        /// <summary>
        /// Removes all elements from the collection
        /// </summary>
        public void Clear()
        {
            BaseClear();
        }

        /// <summary>
        /// Adds a key to the end of the collection
        /// </summary>
        /// <param name="item"></param>
        void ICollection<KeyValuePair<string, TValue>>.Add(KeyValuePair<string, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        /// <summary>
        /// Get an enumerator for the collection in insertion order
        /// </summary>
        /// <returns></returns>
        public new IEnumerator<KeyValuePair<string, TValue>> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return new KeyValuePair<string, TValue>(BaseGetKey(i), (TValue)BaseGet(i));
            }
        }

        bool ICollection<KeyValuePair<string, TValue>>.Contains(KeyValuePair<string, TValue> item)
        {
            for (int i = 0; i < Count; i++)
            {
                if (BaseGetKey(i) == item.Key && item.Value.Equals(BaseGet(i)))
                {
                    return true;
                }
            }

            return false;
        }

        void ICollection<KeyValuePair<string, TValue>>.CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        bool ICollection<KeyValuePair<string, TValue>>.Remove(KeyValuePair<string, TValue> item)
        {
            throw new NotImplementedException();
        }

        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            var keySerializer = new XmlSerializer(typeof(string));
            var valueSerializer = new XmlSerializer(typeof(TValue));

            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();

            if (wasEmpty)
            {
                return;
            }

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                reader.ReadStartElement("Item");

                reader.ReadStartElement("Key");
                string key = (string)keySerializer.Deserialize(reader);
                reader.ReadEndElement();

                reader.ReadStartElement("Value");
                var value = (TValue)valueSerializer.Deserialize(reader);
                reader.ReadEndElement();

                BaseAdd(key, value);

                reader.ReadEndElement();
                reader.MoveToContent();
            }

            reader.ReadEndElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            var keySerializer = new XmlSerializer(typeof(string));
            var valueSerializer = new XmlSerializer(typeof(TValue));

            for (int i = 0; i < Count; i++)
            {
                writer.WriteStartElement("Item");
                writer.WriteAttributeString("index", i.ToString());

                writer.WriteStartElement("Key");
                keySerializer.Serialize(writer, BaseGetKey(i));
                writer.WriteEndElement();

                writer.WriteStartElement("Value");
                var value = (TValue)BaseGet(i);
                valueSerializer.Serialize(writer, value);
                writer.WriteEndElement();

                writer.WriteEndElement();
            }
        }
    }
}
