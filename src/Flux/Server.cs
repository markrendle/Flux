namespace Flux
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using LibuvSharp;
    using LibuvSharp.Threading.Tasks;
    using LibuvSharp.Utilities;
    using Owin;
    using AppFunc = System.Func< // Call
        System.Collections.Generic.IDictionary<string, object>, // Environment
                System.Threading.Tasks.Task>;
    using TcpListener = LibuvSharp.TcpListener;

// Done

    public sealed class Server : IDisposable
    {
        private static readonly byte[] OK = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\n\r\n");
        private AppFunc _app;
        private static readonly IPAddress Localhost = new IPAddress(new byte[] { 127, 0, 0, 1 });
        private readonly TcpListener _listener;
        private readonly IPEndPoint _endPoint;
        private int _started;
        private int _stopped;

        public Server(int port)
            : this(Localhost, port)
        {
        }

        public Server(IPAddress ipAddress, int port)
        {
            _endPoint = new IPEndPoint(ipAddress, port);
            _listener = new TcpListener();
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
        }

        private void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs unobservedTaskExceptionEventArgs)
        {
            Trace.TraceError(unobservedTaskExceptionEventArgs.Exception.Message);
            unobservedTaskExceptionEventArgs.SetObserved();
        }

        public void Start(AppFunc app)
        {
            if (1 != Interlocked.Increment(ref _started)) throw new InvalidOperationException("Server is already started.");

            _app = app;
            _listener.Bind(_endPoint);
            _listener.Connection += ListenerOnConnection;
            _listener.Listen();

            Loop.Default.Run();
        }

        private static Task Temp()
        {
            return Task.FromResult(0);
        }

        private async void ListenerOnConnection()
        {
            var socket = _listener.Accept();
            var cts = new CancellationTokenSource();
            socket.Closed += cts.Cancel;
            var env = await FluxEnvironment.New(socket, RequestScheme.Http, cts.Token);
            if (env != null)
            {
                await _app(env);
                if (!cts.IsCancellationRequested)
                {
                    socket.Write(OK);
                }
                if (socket.Active)
                {
                    socket.Close();
                }
            }
        }

        public void Stop()
        {
            if (_started == 0) return;
            if (1 != Interlocked.Increment(ref _stopped)) return;
            try
            {
                _listener.Close();
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
