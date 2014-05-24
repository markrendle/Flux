namespace Flux
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using AppFunc = System.Func< // Call
        System.Collections.Generic.IDictionary<string, object>, // Environment
                System.Threading.Tasks.Task>; // Done

    public sealed class Server : IDisposable
    {
        private AppFunc _app;
        private static readonly IPAddress Localhost = new IPAddress(new byte[] { 0, 0, 0, 0 });
        private readonly TcpListener _listener;
        private readonly IPAddress _ipAddress;
        private readonly int _port;
        private readonly DataPool _dataPool;
        private readonly TcpServer _tcpServer;
        private int _started;
        private int _stopped;

        public Server(int port)
            : this(Localhost, port)
        {
        }

        public Server(IPAddress ipAddress, int port)
        {
            _ipAddress = ipAddress;
            _port = port;
            _dataPool = new DataPool();
            _tcpServer = new TcpServer(ipAddress, port, _dataPool, TcpServerCallback);
        }

        public void Start(AppFunc app)
        {
            if (1 != Interlocked.Increment(ref _started)) throw new InvalidOperationException("Server is already started.");

            _app = app;
            _tcpServer.Start();
            _listener.Start();
            _listener.BeginAcceptSocket(Callback, null);
        }

        public void Stop()
        {
            if (_started == 0) return;
            if (1 != Interlocked.Increment(ref _stopped)) return;
            try
            {
                _tcpServer.Stop();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        private void TcpServerCallback(Socket socket, int pointer)
        {
            
        }

        private void Callback(IAsyncResult ar)
        {
            Socket socket;
            try
            {
                socket = _listener.EndAcceptSocket(ar);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            _listener.BeginAcceptSocket(Callback, null);
            var instance = new Instance(socket, _app);
            instance.Run()
                .ContinueWith(t =>
                    {
                        if (!t.IsFaulted) return;
                        Trace.TraceError(t.Exception != null ? t.Exception.Message : "A bad thing happened.");
                        instance.TryDispose();
                    });
        }

        public void Dispose()
        {
            Stop();
            _dataPool.Dispose();
        }
    }
}
