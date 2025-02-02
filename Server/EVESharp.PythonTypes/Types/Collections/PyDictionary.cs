using System.Collections;
using System.Collections.Generic;
using EVESharp.PythonTypes.Types.Database;
using EVESharp.PythonTypes.Types.Primitives;
using MySql.Data.MySqlClient;

namespace EVESharp.PythonTypes.Types.Collections
{
    /// <summary>
    /// Special PyDictionary used for iterating normal PyDictionaries
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class PyDictionary<TKey, TValue> : PyDictionary, IPyDictionaryEnumerable<TKey, TValue> where TKey : PyDataType where TValue : PyDataType
    {
        public PyDictionary()
        {
        }

        public PyDictionary(Dictionary<PyDataType, PyDataType> seed) : base(seed)
        {
        }

        public new IPyDictionaryEnumerator<TKey, TValue> GetEnumerator()
        {
            return new PyDictionaryEnumerator<TKey, TValue>(this.mDictionary.GetEnumerator());
        }

        public TValue this[TKey index]
        {
            get => this.mDictionary[index] as TValue;
            set => this.mDictionary[index] = value;
        }

        public static PyDictionary<TKey, TValue> FromMySqlDataReader(IDatabaseConnection connection, MySqlDataReader reader)
        {
            PyDictionary<TKey, TValue> result = new PyDictionary<TKey, TValue>();
            
            for (int i = 0; i < reader.FieldCount; i++)
            {
                FieldType type = connection.GetFieldType(reader, i);
                result[reader.GetName(i)] = IDatabaseConnection.ObjectFromColumn(reader, type, i);
            }

            return result;
        }
    }
    
    public class PyDictionary : PyDataType, IPyDictionaryEnumerable<PyDataType, PyDataType>
    {
        protected readonly Dictionary<PyDataType, PyDataType> mDictionary;

        public PyDictionary()
        {
            this.mDictionary = new Dictionary<PyDataType, PyDataType>();
        }

        public PyDictionary(Dictionary<PyDataType, PyDataType> seed)
        {
            this.mDictionary = seed;
        }

        public bool TryGetValue(PyDataType key, out PyDataType value)
        {
            return this.mDictionary.TryGetValue(key, out value);
        }

        public bool TryGetValue<T>(PyDataType key, out T value) where T : PyDataType
        {
            if (this.TryGetValue(key, out PyDataType tmp) == true)
            {
                value = tmp as T;
                return true;
            }

            value = null;
            return false;
        }

        public void SafeGetValue<T>(PyDataType key, out T value) where T : PyDataType
        {
            if (this.TryGetValue(key, out value) == false)
                throw new KeyNotFoundException();
        }

        public void Add(PyDataType key, PyDataType value)
        {
            this.mDictionary.Add(key, value);
        }

        public bool Remove(PyDataType key)
        {
            return this.mDictionary.Remove(key);
        }

        public bool ContainsKey(PyDataType key)
        {
            return this.mDictionary.ContainsKey(key);
        }

        public bool ContainsValue(PyDataType value)
        {
            return this.mDictionary.ContainsValue(value);
        }

        public PyDataType this[PyDataType index]
        {
            get => this.mDictionary[index];
            set => this.mDictionary[index] = value;
        }

        public PyDictionary<T1, T2> GetEnumerable<T1, T2>() where T1 : PyDataType where T2 : PyDataType
        {
            return new PyDictionary<T1, T2>(this.mDictionary);
        }

        public IPyDictionaryEnumerator<PyDataType, PyDataType> GetEnumerator()
        {
            return new PyDictionaryEnumerator<PyDataType, PyDataType>(this.mDictionary.GetEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new PyDictionaryEnumerator<PyDataType, PyDataType>(this.mDictionary.GetEnumerator());
        }

        public int Length => this.mDictionary.Count;
        public int Count => this.Length;
    }
}