namespace Flux
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using Saea;
    using AppFunc = System.Func< // Call
        System.Collections.Generic.IDictionary<string, object>, // Environment
                System.Threading.Tasks.Task>; // Done

    public sealed class FluxServer : IDisposable
    {
        private readonly IPAddress _ipAddress;
        private readonly int _port;
        private AppFunc _app;
        private static readonly IPAddress Localhost = new IPAddress(new byte[] { 0, 0, 0, 0 });
        private ConnectionPool _connectionPool;
        private readonly Socket _listenSocket;
        private int _started;
        private int _stopped;

        public FluxServer(int port)
            : this(Localhost, port)
        {
        }

        public FluxServer(IPAddress ipAddress, int port)
        {
            _ipAddress = ipAddress;
            _port = port;
            _listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        }

        public void Start(AppFunc app)
        {
            if (1 != Interlocked.Increment(ref _started)) throw new InvalidOperationException("Server is already started.");

            _connectionPool = ConnectionPool.Create(app, 128);
            _listenSocket.Bind(new IPEndPoint(_ipAddress, _port));
            _listenSocket.Listen(100);

            //_connectionPool.Get().Accept(_listenSocket);

            Parallel.Invoke(new ParallelOptions{MaxDegreeOfParallelism = 2}, Accept, Accept);
        }

        private void Accept()
        {
            _connectionPool.Get().Accept(_listenSocket);
        }

        public void Stop()
        {
            if (_started == 0) return;
            if (1 != Interlocked.Increment(ref _stopped)) return;
            _connectionPool.Dispose();
            try
            {
                _listenSocket.Close();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
