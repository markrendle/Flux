using System;
using System.Collections.Generic;
using System.Linq;

namespace Flux
{
    using System.Diagnostics;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using OwinEnvironment = IDictionary<string, object>;
    using OwinHeaders = IDictionary<string, string[]>;
    using ResponseHandler = Func<int, IDictionary<string, string[]>, Func<System.IO.Stream, System.Threading.CancellationToken, System.Threading.Tasks.Task>, System.Threading.Tasks.Task>;
    using App = Func<IDictionary<string, object>, IDictionary<string, string[]>, System.IO.Stream, System.Threading.CancellationToken, Func<int, IDictionary<string, string[]>, Func<System.IO.Stream, System.Threading.CancellationToken, System.Threading.Tasks.Task>, System.Threading.Tasks.Task>, Delegate, System.Threading.Tasks.Task>;
    using Starter = Action<Func<IDictionary<string, object>, IDictionary<string, string[]>, System.IO.Stream, System.Threading.CancellationToken, Func<int, IDictionary<string, string[]>, Func<System.IO.Stream, System.Threading.CancellationToken, System.Threading.Tasks.Task>, System.Threading.Tasks.Task>, Delegate, System.Threading.Tasks.Task>>;

    public class Server
    {
        private readonly App _app;
        private static readonly IPAddress Localhost = new IPAddress(new byte[] { 0, 0, 0, 0 });
        private readonly TcpListener _listener;
        private readonly IPAddress _ipAddress;
        private readonly int _port;
        private int _started = 0;

        public Server(App app, int port)
            : this(app, Localhost, port)
        {
        }

        public Server(App app, IPAddress ipAddress, int port)
        {
            _app = app;
            _ipAddress = ipAddress;
            _port = port;
            _listener = new TcpListener(_ipAddress, _port);
        }

        public void Start()
        {
            if (0 != Interlocked.CompareExchange(ref _started, 1, 0)) throw new InvalidOperationException("Server is already started.");

            _listener.Start();
            _listener.BeginAcceptSocket(Callback, null);
        }

        public void Stop()
        {
            _listener.Stop();
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
                        instance.Dispose();
                        if (t.IsFaulted)
                        {
                            Trace.TraceError(t.Exception.Message);
                        }
                    });
        }
    }
}
