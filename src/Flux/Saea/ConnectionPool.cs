namespace Flux.Saea
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using AppFunc = System.Func<System.Collections.Generic.IDictionary<string,object>, System.Threading.Tasks.Task>;

    public class ConnectionPool : IDisposable
    {
        private readonly object _sync = new object();
        private readonly Stack<Connection> _connections;
        private readonly Buffer _acceptBuffer;
        private readonly Buffer _receiveBuffer;
        private readonly Buffer _sendBuffer;
        private readonly Buffer _disconnectBuffer;
        private readonly AppFunc _appFunc;
        private bool _disposed;

        private ConnectionPool(Buffer acceptBuffer, Buffer receiveBuffer, Buffer sendBuffer, Buffer disconnectBuffer, AppFunc appFunc)
        {
            _acceptBuffer = acceptBuffer;
            _receiveBuffer = receiveBuffer;
            _sendBuffer = sendBuffer;
            _disconnectBuffer = disconnectBuffer;
            _appFunc = appFunc;
            _connections = new Stack<Connection>();
        }

        public static ConnectionPool Create(AppFunc appFunc, int initialSize)
        {
            var acceptBuffer = new Buffer(1024 * initialSize, 1024);
            var receiveBuffer = new Buffer(1024 * initialSize, 1024);
            var sendBuffer = new Buffer(1024 * initialSize, 1024);
            var disconnectBuffer = new Buffer(128 * initialSize, 128);
            var pool = new ConnectionPool(acceptBuffer, receiveBuffer, sendBuffer, disconnectBuffer, appFunc);
            for (int i = 0; i < initialSize; i++)
            {
                pool._connections.Push(new Connection(appFunc, acceptBuffer, receiveBuffer, sendBuffer, disconnectBuffer, pool));
            }
            return pool;
        }

        public Connection Get()
        {
            lock (_sync)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException("connectionPool");
                }
                if (_connections.Count > 0)
                {
                    return _connections.Pop();
                }
            }
            Console.WriteLine("ConnectionPool missed");
            return new Connection(_appFunc, _acceptBuffer, _receiveBuffer, _sendBuffer, _disconnectBuffer, this);
        }

        public void Release(Connection connection)
        {
            lock (_sync)
            {
                if (_disposed)
                {
                    connection.Dispose();
                    return;
                }
                _connections.Push(connection);
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                _disposed = true;
                while (_connections.Count > 0)
                {
                    _connections.Pop().Dispose();
                }
            }
        }
    }
}