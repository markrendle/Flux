namespace Flux
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
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
        private static readonly IPAddress Localhost = new IPAddress(new byte[] { 127, 0, 0, 1 });
        private readonly Loop _loop;
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
            _loop = new Loop(new FluxByteBufferAllocator());
            _listener = new TcpListener(_loop);
        }

        public void Start(AppFunc app)
        {
            if (1 != Interlocked.Increment(ref _started)) throw new InvalidOperationException("Server is already started.");

            Instance.AppFunc = app;
            Instance.RequestScheme = RequestScheme.Http;
            _listener.Bind(_endPoint);
            _listener.Connection += ListenerOnConnection;
            _listener.Listen();

            _loop.Run();
        }

        private void ListenerOnConnection()
        {
            Instance.Allocate(_listener.Accept());
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
