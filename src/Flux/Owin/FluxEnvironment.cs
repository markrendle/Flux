using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Flux.Owin
{
    public partial class FluxEnvironment : IDictionary<string, object>
    {
        private const byte NewLine = (byte) '\n';
        private readonly Socket _socket;
        private readonly byte[] _buffer;
        private readonly int _offset;
        private readonly int _requestLineCount;
        private readonly int _headerEnd;

        public FluxEnvironment(Socket socket, byte[] buffer, int offset, int headerEnd)
        {
            _socket = socket;
            _buffer = buffer;
            _offset = offset;
            _requestLineCount = Array.IndexOf(_buffer, NewLine, _offset) - _offset;
            _headerEnd = headerEnd;
        }

        public object this[string key]
        {
            get { return Get(key); }
            set { throw new System.NotImplementedException(); }
        }



        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            throw new System.NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<string, object> item)
        {
            throw new System.NotImplementedException();
        }

        public void Clear()
        {
            throw new System.NotImplementedException();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            throw new System.NotImplementedException();
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            throw new System.NotImplementedException();
        }

        public int Count { get; private set; }
        public bool IsReadOnly { get; private set; }
        public bool ContainsKey(string key)
        {
            throw new System.NotImplementedException();
        }

        public void Add(string key, object value)
        {
            throw new System.NotImplementedException();
        }

        public bool Remove(string key)
        {
            throw new System.NotImplementedException();
        }

        public bool TryGetValue(string key, out object value)
        {
            throw new System.NotImplementedException();
        }

        public ICollection<string> Keys { get; private set; }
        public ICollection<object> Values { get; private set; }
    }
}