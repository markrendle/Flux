﻿namespace Flux.Owin
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using LibuvSharp;
    using LibuvSharp.Threading.Tasks;

    public partial class FluxEnvironment : IDictionary<string, object>
    {
        private const byte CarriageReturn = (byte)'\r';
        private const byte NewLine = (byte)'\n';
        private readonly Tcp _socket;
        private readonly List<ArraySegment<byte>> _data;
        private readonly int _requestLineCount;
        private readonly IDictionary<string, object> _internal = new Dictionary<string, object>();
        private readonly object _syncRoot;

        public static Task<FluxEnvironment> New(Tcp socket, RequestScheme requestScheme, CancellationToken callCancellationToken)
        {
            var tcs = new TaskCompletionSource<FluxEnvironment>();
            socket.Data +=
                bytes =>
                {
                    if (Array.IndexOf(bytes.Array, CarriageReturn, bytes.Offset, bytes.Count) < 0)
                    {
                        tcs.SetException(new FluxNetworkException("Incomplete request"));
                    }
                    else
                    {
                        tcs.SetResult(new FluxEnvironment(socket, bytes, requestScheme, callCancellationToken));
                    }
                };
            socket.Resume();
            return tcs.Task;
        }

        public FluxEnvironment(Tcp socket, ArraySegment<byte> data, RequestScheme requestScheme, CancellationToken callCancellationToken)
        {
            _socket = socket;
            _socket.Data += SocketOnData;
            _data = new List<ArraySegment<byte>>(16){};
            _requestLineCount = Array.IndexOf(data.Array, CarriageReturn, data.Offset, data.Count) - data.Offset;
            _internal = new Dictionary<string, object>(32)
            {
                {OwinKeys.Version, "1.0"},
                {OwinKeys.CallCancelled, callCancellationToken},
                {OwinKeys.RequestScheme, requestScheme == RequestScheme.Http ? "http" : "https"}
            };
            _syncRoot = ((ICollection)_internal).SyncRoot;

        }

        private void SocketOnData(ArraySegment<byte> data)
        {
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

    [Serializable]
    public class FluxNetworkException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public FluxNetworkException()
        {
        }

        public FluxNetworkException(string message) : base(message)
        {
        }

        public FluxNetworkException(string message, Exception inner) : base(message, inner)
        {
        }

        protected FluxNetworkException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}