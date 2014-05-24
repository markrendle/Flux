using System.Threading;

namespace Flux
{
    using System;
    using System.Net;
    using System.Net.Sockets;

    /// <summary>
    /// TCP Server.
    /// </summary>
    public class TcpServer
    {
        private static readonly IPAddress Localhost = new IPAddress(new byte[] { 0, 0, 0, 0 });
        private readonly Socket _listener;
        private readonly DataPool _dataPool;
        private readonly Action<Socket, int> _callback;
        private int _started;
        private int _stopped;
        private readonly IPEndPoint _endpoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpServer"/> class.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        /// <param name="bot">The Buffer object.</param>
        /// <param name="callback">Action to call with new connections.</param>
        public TcpServer(DataPool bot, Action<Socket, int> callback) : this(5875, bot, callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpServer"/> class.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        /// <param name="bot">The Buffer object.</param>
        /// <param name="callback">Action to call with new connections.</param>
        public TcpServer(int port, DataPool bot, Action<Socket, int> callback) : this(Localhost, port, bot, callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpServer"/> class.
        /// </summary>
        /// <param name="ipAddress">The IP address.</param>
        /// <param name="port">The port to listen on.</param>
        /// <param name="dataPool">The Buffer object.</param>
        /// <param name="callback">Action to call with new connections.</param>
        public TcpServer(IPAddress ipAddress, int port, DataPool dataPool, Action<Socket, int> callback)
        {
            if (dataPool == null) throw new ArgumentNullException("dataPool");
            if (callback == null) throw new ArgumentNullException("callback");
            _dataPool = dataPool;
            _callback = callback;
            _endpoint = new IPEndPoint(ipAddress, port);
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //_bot = new BufferBot(1024 * 1024, 1024);
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        public void Start()
        {
            if (Interlocked.CompareExchange(ref _started, 1, 0) != 0)
            {
                throw new InvalidOperationException("Already started.");
            }
            _listener.Bind(_endpoint);
            _listener.Listen(100);
            _listener.BeginAccept(Accepted, null);
        }

        private void Accepted(IAsyncResult ar)
        {
            var newSocket = _listener.EndAccept(ar);
            var pointer = _dataPool.GetNextPointer();
            _listener.BeginAccept(Accepted, null);
            newSocket.BeginReceive(_dataPool.Buffer, pointer, 1024, SocketFlags.None, Received,
                new SocketInfo(newSocket, pointer));
        }

        private void Received(IAsyncResult ar)
        {
            var socketInfo = (SocketInfo) ar.AsyncState;
            _callback(socketInfo.Socket, socketInfo.Pointer);
        }

        private class SocketInfo
        {
            public readonly Socket Socket;
            public readonly int Pointer;

            public SocketInfo(Socket socket, int pointer)
            {
                Socket = socket;
                Pointer = pointer;
            }
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}