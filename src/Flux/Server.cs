﻿namespace Flux
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using App = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Collections.Generic.IDictionary<string, string[]>, System.IO.Stream, System.Threading.CancellationToken, System.Func<int, System.Collections.Generic.IDictionary<string, string[]>, System.Func<System.IO.Stream, System.Threading.CancellationToken, System.Threading.Tasks.Task>, System.Threading.Tasks.Task>, System.Delegate, System.Threading.Tasks.Task>;

    public sealed class Server : IDisposable
    {
        private App _app;
        private static readonly IPAddress Localhost = new IPAddress(new byte[] { 0, 0, 0, 0 });
        private readonly TcpListener _listener;
        private readonly IPAddress _ipAddress;
        private readonly int _port;
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
            _listener = new TcpListener(_ipAddress, _port);
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
        }

        private void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs unobservedTaskExceptionEventArgs)
        {
            Trace.TraceError(unobservedTaskExceptionEventArgs.Exception.Message);
            unobservedTaskExceptionEventArgs.SetObserved();
        }

        public void Start(App app)
        {
            if (0 != Interlocked.CompareExchange(ref _started, 1, 0)) throw new InvalidOperationException("Server is already started.");

            _app = app;
            _listener.Start();
            _listener.BeginAcceptTcpClient(Callback, null);
        }

        public void Stop()
        {
            if (_started == 0) return;
            if (0 != Interlocked.CompareExchange(ref _stopped, 1, 0)) return;
            try
            {
                _listener.Stop();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        private void Callback(IAsyncResult ar)
        {
            TcpClient socket;
            try
            {
                socket = _listener.EndAcceptTcpClient(ar);
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
                        if (t.IsFaulted)
                        {
                            Trace.TraceError(t.Exception != null ? t.Exception.Message : "A bad thing happened.");
                        }
                    });
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
