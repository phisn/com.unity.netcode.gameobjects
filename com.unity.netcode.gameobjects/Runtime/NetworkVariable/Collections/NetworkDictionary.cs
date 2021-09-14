using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Unity.Netcode
{
    /// <summary>
    /// Event based NetworkVariable container for syncing Dictionaries
    /// </summary>
    /// <typeparam name="TKey">The type for the dictionary keys</typeparam>
    /// <typeparam name="TValue">The type for the dictionary values</typeparam>
    public class NetworkDictionary<TKey, TValue> : NetworkVariableBase, IDictionary<TKey, TValue> where TKey : unmanaged where TValue : unmanaged
    {
        private readonly IDictionary<TKey, TValue> m_Dictionary = new Dictionary<TKey, TValue>();
        private readonly List<NetworkDictionaryEvent<TKey, TValue>> m_DirtyEvents = new List<NetworkDictionaryEvent<TKey, TValue>>();

        /// <summary>
        /// Delegate type for dictionary changed event
        /// </summary>
        /// <param name="changeEvent">Struct containing information about the change event</param>
        public delegate void OnDictionaryChangedDelegate(NetworkDictionaryEvent<TKey, TValue> changeEvent);

        /// <summary>
        /// The callback to be invoked when the dictionary gets changed
        /// </summary>
        public event OnDictionaryChangedDelegate OnDictionaryChanged;

        /// <summary>
        /// Creates a NetworkDictionary with the default value and settings
        /// </summary>
        public NetworkDictionary() { }

        /// <summary>
        /// Creates a NetworkDictionary with the default value and custom settings
        /// </summary>
        /// <param name="readPerm">The read permission to use for this NetworkDictionary</param>
        public NetworkDictionary(NetworkVariableReadPermission readPerm) : base(readPerm) { }

        /// <summary>
        /// Creates a NetworkDictionary with a custom value and custom settings
        /// </summary>
        /// <param name="readPerm">The read permission to use for this NetworkDictionary</param>
        /// <param name="value">The initial value to use for the NetworkDictionary</param>
        public NetworkDictionary(NetworkVariableReadPermission readPerm, IDictionary<TKey, TValue> value) : base(readPerm)
        {
            m_Dictionary = value;
        }

        /// <summary>
        /// Creates a NetworkDictionary with a custom value and the default settings
        /// </summary>
        /// <param name="value">The initial value to use for the NetworkDictionary</param>
        public NetworkDictionary(IDictionary<TKey, TValue> value)
        {
            m_Dictionary = value;
        }

        /// <inheritdoc />
        public override void ResetDirty()
        {
            base.ResetDirty();
            m_DirtyEvents.Clear();
        }

        /// <inheritdoc />
        public override void ReadDelta(ref FastBufferReader reader, bool keepDirtyDelta)
        {
            reader.ReadValueSafe(out ushort deltaCount);
            for (int i = 0; i < deltaCount; i++)
            {
                reader.ReadValueSafe(out NetworkDictionaryEvent<TKey, TValue>.EventType eventType);
                switch (eventType)
                {
                    case NetworkDictionaryEvent<TKey, TValue>.EventType.Add:
                        {
                            reader.ReadValueSafe(out TKey key);
                            reader.ReadValueSafe(out TValue value);
                            m_Dictionary.Add(key, value);

                            if (OnDictionaryChanged != null)
                            {
                                OnDictionaryChanged(new NetworkDictionaryEvent<TKey, TValue>
                                {
                                    Type = eventType,
                                    Key = key,
                                    Value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                m_DirtyEvents.Add(new NetworkDictionaryEvent<TKey, TValue>()
                                {
                                    Type = eventType,
                                    Key = key,
                                    Value = value
                                });
                            }
                        }
                        break;
                    case NetworkDictionaryEvent<TKey, TValue>.EventType.Remove:
                        {
                            reader.ReadValueSafe(out TKey key);
                            TValue value;
                            m_Dictionary.TryGetValue(key, out value);
                            m_Dictionary.Remove(key);

                            if (OnDictionaryChanged != null)
                            {
                                OnDictionaryChanged(new NetworkDictionaryEvent<TKey, TValue>
                                {
                                    Type = eventType,
                                    Key = key,
                                    Value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                m_DirtyEvents.Add(new NetworkDictionaryEvent<TKey, TValue>()
                                {
                                    Type = eventType,
                                    Key = key,
                                    Value = value
                                });
                            }
                        }
                        break;
                    case NetworkDictionaryEvent<TKey, TValue>.EventType.RemovePair:
                        {
                            reader.ReadValueSafe(out TKey key);
                            reader.ReadValueSafe(out TValue value);
                            m_Dictionary.Remove(new KeyValuePair<TKey, TValue>(key, value));

                            if (OnDictionaryChanged != null)
                            {
                                OnDictionaryChanged(new NetworkDictionaryEvent<TKey, TValue>
                                {
                                    Type = eventType,
                                    Key = key,
                                    Value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                m_DirtyEvents.Add(new NetworkDictionaryEvent<TKey, TValue>()
                                {
                                    Type = eventType,
                                    Key = key,
                                    Value = value
                                });
                            }
                        }
                        break;
                    case NetworkDictionaryEvent<TKey, TValue>.EventType.Clear:
                        {
                            //read nothing
                            m_Dictionary.Clear();

                            if (OnDictionaryChanged != null)
                            {
                                OnDictionaryChanged(new NetworkDictionaryEvent<TKey, TValue>
                                {
                                    Type = eventType
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                m_DirtyEvents.Add(new NetworkDictionaryEvent<TKey, TValue>
                                {
                                    Type = eventType
                                });
                            }
                        }
                        break;
                    case NetworkDictionaryEvent<TKey, TValue>.EventType.Value:
                        {
                            reader.ReadValueSafe(out TKey key);
                            reader.ReadValueSafe(out TValue value);

                            m_Dictionary[key] = value;

                            if (OnDictionaryChanged != null)
                            {
                                OnDictionaryChanged(new NetworkDictionaryEvent<TKey, TValue>
                                {
                                    Type = eventType,
                                    Key = key,
                                    Value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                m_DirtyEvents.Add(new NetworkDictionaryEvent<TKey, TValue>()
                                {
                                    Type = eventType,
                                    Key = key,
                                    Value = value
                                });
                            }
                        }
                        break;
                }
            }
        }

        /// <inheritdoc />
        public override void ReadField(ref FastBufferReader reader)
        {
            m_Dictionary.Clear();
            reader.ReadValueSafe(out ushort entryCount);
            for (int i = 0; i < entryCount; i++)
            {
                reader.ReadValueSafe(out TKey key);
                reader.ReadValueSafe(out TValue value);
                m_Dictionary.Add(key, value);
            }
        }

        /// <inheritdoc />
        public bool TryGetValue(TKey key, out TValue value)
        {
            return m_Dictionary.TryGetValue(key, out value);
        }

        /// <inheritdoc />
        public override void WriteDelta(ref FastBufferWriter writer)
        {
            writer.WriteValueSafe((ushort)m_DirtyEvents.Count);
            for (int i = 0; i < m_DirtyEvents.Count; i++)
            {
                writer.WriteValueSafe(m_DirtyEvents[i].Type);
                switch (m_DirtyEvents[i].Type)
                {
                    case NetworkDictionaryEvent<TKey, TValue>.EventType.Add:
                        {
                            writer.WriteValueSafe(m_DirtyEvents[i].Key);
                            writer.WriteValueSafe(m_DirtyEvents[i].Value);
                        }
                        break;
                    case NetworkDictionaryEvent<TKey, TValue>.EventType.Remove:
                        {
                            writer.WriteValueSafe(m_DirtyEvents[i].Key);
                        }
                        break;
                    case NetworkDictionaryEvent<TKey, TValue>.EventType.RemovePair:
                        {
                            writer.WriteValueSafe(m_DirtyEvents[i].Key);
                            writer.WriteValueSafe(m_DirtyEvents[i].Value);
                        }
                        break;
                    case NetworkDictionaryEvent<TKey, TValue>.EventType.Clear:
                        {
                            //write nothing
                        }
                        break;
                    case NetworkDictionaryEvent<TKey, TValue>.EventType.Value:
                        {
                            writer.WriteValueSafe(m_DirtyEvents[i].Key);
                            writer.WriteValueSafe(m_DirtyEvents[i].Value);
                        }
                        break;
                }
            }
        }

        /// <inheritdoc />
        public override void WriteField(ref FastBufferWriter writer)
        {
            writer.WriteValueSafe((ushort)m_Dictionary.Count);
            foreach (KeyValuePair<TKey, TValue> pair in m_Dictionary)
            {
                writer.WriteValueSafe(pair.Key);
                writer.WriteValueSafe(pair.Value);
            }
        }

        /// <inheritdoc />
        public override bool IsDirty()
        {
            return base.IsDirty() || m_DirtyEvents.Count > 0;
        }

        /// <inheritdoc />
        public TValue this[TKey key]
        {
            get => m_Dictionary[key];
            set
            {
                m_Dictionary[key] = value;

                var dictionaryEvent = new NetworkDictionaryEvent<TKey, TValue>()
                {
                    Type = NetworkDictionaryEvent<TKey, TValue>.EventType.Value,
                    Key = key,
                    Value = value
                };

                HandleAddDictionaryEvent(dictionaryEvent);
            }
        }

        /// <inheritdoc />
        public ICollection<TKey> Keys => m_Dictionary.Keys;

        /// <inheritdoc />
        public ICollection<TValue> Values => m_Dictionary.Values;

        /// <inheritdoc />
        public int Count => m_Dictionary.Count;

        /// <inheritdoc />
        // TODO: remove w/ native containers
        public bool IsReadOnly => m_Dictionary.IsReadOnly;

        /// <inheritdoc />
        public void Add(TKey key, TValue value)
        {
            m_Dictionary.Add(key, value);

            var dictionaryEvent = new NetworkDictionaryEvent<TKey, TValue>()
            {
                Type = NetworkDictionaryEvent<TKey, TValue>.EventType.Add,
                Key = key,
                Value = value
            };

            HandleAddDictionaryEvent(dictionaryEvent);
        }

        /// <inheritdoc />
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            m_Dictionary.Add(item);

            var dictionaryEvent = new NetworkDictionaryEvent<TKey, TValue>()
            {
                Type = NetworkDictionaryEvent<TKey, TValue>.EventType.Add,
                Key = item.Key,
                Value = item.Value
            };

            HandleAddDictionaryEvent(dictionaryEvent);
        }

        /// <inheritdoc />
        public void Clear()
        {
            m_Dictionary.Clear();

            var dictionaryEvent = new NetworkDictionaryEvent<TKey, TValue>()
            {
                Type = NetworkDictionaryEvent<TKey, TValue>.EventType.Clear
            };

            HandleAddDictionaryEvent(dictionaryEvent);
        }

        /// <inheritdoc />
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return m_Dictionary.Contains(item);
        }

        /// <inheritdoc />
        public bool ContainsKey(TKey key)
        {
            return m_Dictionary.ContainsKey(key);
        }

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            m_Dictionary.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return m_Dictionary.GetEnumerator();
        }

        /// <inheritdoc />
        public bool Remove(TKey key)
        {
            m_Dictionary.Remove(key);

            TValue value;
            m_Dictionary.TryGetValue(key, out value);

            var dictionaryEvent = new NetworkDictionaryEvent<TKey, TValue>()
            {
                Type = NetworkDictionaryEvent<TKey, TValue>.EventType.Remove,
                Key = key,
                Value = value
            };

            HandleAddDictionaryEvent(dictionaryEvent);

            return true;
        }


        /// <inheritdoc />
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            m_Dictionary.Remove(item);

            var dictionaryEvent = new NetworkDictionaryEvent<TKey, TValue>()
            {
                Type = NetworkDictionaryEvent<TKey, TValue>.EventType.RemovePair,
                Key = item.Key,
                Value = item.Value
            };

            HandleAddDictionaryEvent(dictionaryEvent);
            return true;
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_Dictionary.GetEnumerator();
        }

        private void HandleAddDictionaryEvent(NetworkDictionaryEvent<TKey, TValue> dictionaryEvent)
        {
            m_DirtyEvents.Add(dictionaryEvent);
            OnDictionaryChanged?.Invoke(dictionaryEvent);
        }

        public int LastModifiedTick
        {
            get
            {
                // todo: implement proper network tick for NetworkDictionary
                return NetworkTickSystem.NoTick;
            }
        }
    }

    /// <summary>
    /// Struct containing event information about changes to a NetworkDictionary.
    /// </summary>
    /// <typeparam name="TKey">The type for the dictionary key that the event is about</typeparam>
    /// <typeparam name="TValue">The type for the dictionary value that the event is about</typeparam>
    public struct NetworkDictionaryEvent<TKey, TValue>
    {
        /// <summary>
        /// Enum representing the different operations available for triggering an event.
        /// </summary>
        public enum EventType : byte
        {
            /// <summary>
            /// Add
            /// </summary>
            Add,

            /// <summary>
            /// Remove
            /// </summary>
            Remove,

            /// <summary>
            /// Remove pair
            /// </summary>
            RemovePair,

            /// <summary>
            /// Clear
            /// </summary>
            Clear,

            /// <summary>
            /// Value changed
            /// </summary>
            Value
        }

        /// <summary>
        /// Enum representing the operation made to the dictionary.
        /// </summary>
        public EventType Type;

        /// <summary>
        /// the key changed, added or removed if available.
        /// </summary>
        public TKey Key;

        /// <summary>
        /// The value changed, added or removed if available.
        /// </summary>
        public TValue Value;
    }
}
