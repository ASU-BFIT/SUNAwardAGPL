using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace BTS.Common
{
    /// <summary>
    /// Dictionary which can be serialized to XML
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [XmlRoot("Dictionary"), Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
    {
        /// <summary>
        /// Constructs a new empty dictionary
        /// </summary>
        public SerializableDictionary() : base() { }

        /// <summary>
        /// Constructs a new empty dictionary with initial capacity
        /// </summary>
        /// <param name="capacity"></param>
        public SerializableDictionary(int capacity) : base(capacity) { }

        /// <summary>
        /// Constructs a new empty dictionary with the specified equality comparer
        /// </summary>
        /// <param name="comparer"></param>
        public SerializableDictionary(IEqualityComparer<TKey> comparer) : base(comparer) { }

        /// <summary>
        /// Copies a dictionary into ours
        /// </summary>
        /// <param name="dictionary"></param>
        public SerializableDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary) { }

        /// <summary>
        /// Constructs a new empty dictionary with the specified capacity and equality comparer
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="comparer"></param>
        public SerializableDictionary(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer) { }

        /// <summary>
        /// Deserializes a dictionary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected SerializableDictionary(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <summary>
        /// Copies a dictionary using the specified equality comparer
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="comparer"></param>
        public SerializableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) : base(dictionary, comparer) { }

        /// <summary>
        /// Returns null (we do not have an XML schema for SerializableDictionary)
        /// </summary>
        /// <returns></returns>
        public XmlSchema GetSchema()
        {
            return null;
        }

        /// <summary>
        /// Populates dictionary from XML
        /// </summary>
        /// <param name="reader"></param>
        public void ReadXml(XmlReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            var keySerializer = new XmlSerializer(typeof(TKey));
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
                var key = (TKey)keySerializer.Deserialize(reader);
                reader.ReadEndElement();

                reader.ReadStartElement("Value");
                var value = (TValue)valueSerializer.Deserialize(reader);
                reader.ReadEndElement();

                Add(key, value);

                reader.ReadEndElement();
                reader.MoveToContent();
            }

            reader.ReadEndElement();
        }

        /// <summary>
        /// Serializes dictionary to XML
        /// </summary>
        /// <param name="writer"></param>
        public void WriteXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            var keySerializer = new XmlSerializer(typeof(TKey));
            var valueSerializer = new XmlSerializer(typeof(TValue));

            foreach (var key in Keys)
            {
                writer.WriteStartElement("Item");

                writer.WriteStartElement("Key");
                keySerializer.Serialize(writer, key);
                writer.WriteEndElement();

                writer.WriteStartElement("Value");
                var value = this[key];
                valueSerializer.Serialize(writer, value);
                writer.WriteEndElement();

                writer.WriteEndElement();
            }
        }
    }
}
