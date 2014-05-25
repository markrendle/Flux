namespace Flux
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading;
    using Owin;
    using AppFunc = System.Func< // Call
        System.Collections.Generic.IDictionary<string, object>, // Environment
                System.Threading.Tasks.Task>; // Done

    public sealed class FluxServer : IDisposable
    {
        private AppFunc _app;
        private static readonly IPAddress Localhost = new IPAddress(new byte[] { 0, 0, 0, 0 });
        private readonly DataPool _dataPool;
        private readonly TcpServer _tcpServer;
        private int _started;
        private int _stopped;

        public FluxServer(int port)
            : this(Localhost, port)
        {
        }

        public FluxServer(IPAddress ipAddress, int port)
        {
            _dataPool = new DataPool();
            _tcpServer = new TcpServer(ipAddress, port, _dataPool, TcpServerCallback);
        }

        public void Start(AppFunc app)
        {
            if (1 != Interlocked.Increment(ref _started)) throw new InvalidOperationException("Server is already started.");

            _app = app;
            _tcpServer.Start();
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

        private void TcpServerCallback(Socket socket, ArraySegment<byte> segment)
        {
            var cancellation = new CancellationTokenSource();
            var env = new FluxEnvironment(socket, segment, RequestScheme.Http, cancellation.Token);
            _app(env).ContinueWith(t =>
            {
                var buffer = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\n\r\n");
                socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, Sent, socket);
            }, cancellation.Token);
        }

        private void Sent(IAsyncResult ar)
        {
            var socket = (Socket) ar.AsyncState;
            int sent = socket.EndSend(ar);
            socket.BeginDisconnect(false, Disconnected, socket);
        }

        private void Disconnected(IAsyncResult ar)
        {
            var socket = (Socket) ar.AsyncState;
            socket.EndDisconnect(ar);
        }

        public void Dispose()
        {
            Stop();
            _dataPool.Dispose();
        }
    }
}
