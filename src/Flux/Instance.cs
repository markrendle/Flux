using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Flux.Owin;
using LibuvSharp;

namespace Flux
{
    internal class Instance
    {
        private static readonly byte[] OK = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\n\r\n");
        private static readonly ConcurrentStack<Instance> Pool = new ConcurrentStack<Instance>(Enumerable.Range(0, 64).Select(_ => new Instance()));
        private readonly FluxEnvironment _environment = new FluxEnvironment(RequestScheme);
        private readonly List<ArraySegment<byte>> _data = new List<ArraySegment<byte>>(16);
        private Tcp _socket;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Action<FluxEnvironment, Exception> _callback;
        public static RequestScheme RequestScheme { get; set; }
        public static Func<IDictionary<string, object>, Task> AppFunc { get; set; }

        public static void Allocate(Tcp socket)
        {
            Instance instance;
            if (!Pool.TryPop(out instance))
            {
                instance = new Instance();
            }
            instance.Init(socket);
        }

        private void Init(Tcp socket)
        {
            _socket = socket;
            _socket.SetDataCallback(SocketOnFirstData);
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource = new CancellationTokenSource();
            }
            socket.Closed += _cancellationTokenSource.Cancel;
            _socket.Resume();
        }

        public void Free()
        {
            _socket.SetDataCallback(null);
            _socket = null;
            _environment.Reset();
            for (int i = 0; i < _data.Count; i++)
            {
                BytePool.Intance.Free(_data[i]);
            }
        }

        private async void SocketOnFirstData(ArraySegment<byte> bytes)
        {
            _socket.SetDataCallback(SocketOnData);
            if (Array.IndexOf(bytes.Array, NamedBytes.CarriageReturn, bytes.Offset, bytes.Count) < 0)
            {
                throw new FluxNetworkException("Incomplete request");
            }
            await AppFunc(_environment);
            if (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                _socket.Write(OK);
            }
            if (_socket.Active)
            {
                _socket.Close();
            }

            Free();
        }

        private void SocketOnData(ArraySegment<byte> bytes)
        {
            _data.Add(bytes);
        }
    }
}