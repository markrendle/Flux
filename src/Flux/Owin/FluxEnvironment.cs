using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Flux.Owin
{
    using System.Threading;

    public partial class FluxEnvironment : IDictionary<string, object>
    {
        private const byte NewLine = (byte) '\n';
        private readonly Socket _socket;
        private readonly ArraySegment<byte> _data;
        private readonly int _requestLineCount;
        private readonly IDictionary<string, object> _internal = new Dictionary<string, object>();
        private readonly object _syncRoot;

        public FluxEnvironment(Socket socket, ArraySegment<byte> data, RequestScheme requestScheme, CancellationToken callCancellationToken)
        {
            _socket = socket;
            _data = data;
            _requestLineCount = Array.IndexOf(data.Array, NewLine, data.Offset, data.Count) - data.Offset;
            _internal = new Dictionary<string, object>(32)
            {
                {OwinKeys.Version, "1.0"},
                {OwinKeys.CallCancelled, callCancellationToken},
                {OwinKeys.RequestScheme, requestScheme == RequestScheme.Http ? "http" : "https"}
            };
            _syncRoot = ((ICollection) _internal).SyncRoot;

        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _internal.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<string, object> item)
        {
            _internal.Add(item);
        }

        public void Clear()
        {
            _internal.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return _internal.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            _internal.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return _internal.Remove(item);
        }

        public int Count
        {
            get { return _internal.Count; }
        }

        public bool IsReadOnly
        {
            get { return _internal.IsReadOnly; }
        }

        public void Add(string key, object value)
        {
            _internal.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return _internal.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            return _internal.Remove(key);
        }

        public bool TryGetValue(string key, out object value)
        {
            return _internal.TryGetValue(key, out value);
        }

        public object this[string key]
        {
            get
            {
                object value;
                if (_internal.TryGetValue(key, out value)) return value;
                ParseKey(key);
                return _internal[key];
            }
            set { _internal[key] = value; }
        }

        public ICollection<string> Keys
        {
            get { return _internal.Keys; }
        }

        public ICollection<object> Values
        {
            get { return _internal.Values; }
        }
    }
}