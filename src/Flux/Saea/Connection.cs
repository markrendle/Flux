namespace Flux.Saea
{
    using System;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using Owin;
    using AppFunc = System.Func<System.Collections.Generic.IDictionary<string,object>, System.Threading.Tasks.Task>;

    public class Connection : IDisposable
    {
        private readonly AppFunc _appFunc;
        private readonly ConnectionPool _pool;
        private readonly SocketAsyncEventArgs _acceptArgs;
        private readonly SocketAsyncEventArgs _receiveArgs;
        private readonly SocketAsyncEventArgs _sendArgs;
        private readonly SocketAsyncEventArgs _disconnectArgs;
        private Socket _listener;
        private readonly Socket _socket;
        private static readonly byte[] OK = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\n\r\n");

        public Connection(AppFunc appFunc, Buffer acceptBuffer, Buffer receiveBuffer, Buffer sendBuffer, Buffer disconnectBuffer, ConnectionPool pool)
        {
            _appFunc = appFunc;
            _pool = pool;

            _acceptArgs = new SocketAsyncEventArgs();
            _acceptArgs.Completed += AcceptArgsOnCompleted;
            _acceptArgs.AcceptSocket = _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            acceptBuffer.Alloc(_acceptArgs);


            _receiveArgs = new SocketAsyncEventArgs();
            _receiveArgs.Completed += ReceiveArgsOnCompleted;
            receiveBuffer.Alloc(_receiveArgs);

            _sendArgs = new SocketAsyncEventArgs();
            _sendArgs.Completed += SendArgsOnCompleted;
            sendBuffer.Alloc(_sendArgs);

            _disconnectArgs = new SocketAsyncEventArgs {DisconnectReuseSocket = true};
            _disconnectArgs.Completed += DisconnectArgsOnCompleted;
            disconnectBuffer.Alloc(_disconnectArgs);
        }

        private void DisconnectArgsOnCompleted(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        {
            Disconnected();
        }

        private void SendArgsOnCompleted(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        {
            Sent();
        }

        private void ReceiveArgsOnCompleted(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        {
            Received();
        }

        private void AcceptArgsOnCompleted(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        {
            Accepted();
        }

        public void Accept(Socket listener)
        {
            _listener = listener;
            if (!listener.AcceptAsync(_acceptArgs))
            {
                Accepted();
            }
        }

        private void Accepted()
        {
            ThreadPool.QueueUserWorkItem(Next);
            var cancellation = new CancellationTokenSource();
            var receivedSegment = new ArraySegment<byte>(_acceptArgs.Buffer, _acceptArgs.Offset, _acceptArgs.BytesTransferred);
            var env = new FluxEnvironment(_socket, receivedSegment, RequestScheme.Http, cancellation.Token);
            _appFunc(env).ContinueWith(t =>
            {
                OK.CopyTo(_sendArgs.Buffer, _sendArgs.Offset);
                _sendArgs.SetBuffer(_sendArgs.Offset, OK.Length);
                if (!_socket.SendAsync(_sendArgs))
                {
                    Sent();
                }
            }, cancellation.Token);
        }

        private void Next(object state)
        {
            _pool.Get().Accept(_listener);
        }

        private void Received()
        {
            var cancellation = new CancellationTokenSource();
            var receivedSegment = new ArraySegment<byte>(_receiveArgs.Buffer, _receiveArgs.Offset, _receiveArgs.Count);
            var env = new FluxEnvironment(_socket, receivedSegment, RequestScheme.Http, cancellation.Token);
            _appFunc(env).ContinueWith(t =>
            {
                int count = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\n\r\n", 0, 1024, _sendArgs.Buffer, _sendArgs.Offset);
                _sendArgs.SetBuffer(_sendArgs.Offset, count);
                if (!_socket.SendAsync(_sendArgs))
                {
                    Sent();
                }
            }, cancellation.Token);
        }

        private void Sent()
        {
            if (!_socket.DisconnectAsync(_disconnectArgs))
            {
                Disconnected();
            }
        }

        private void Disconnected()
        {
            _pool.Release(this);
        }

        public void Dispose()
        {
            TryDispose(_socket);
            TryDispose(_acceptArgs);
            TryDispose(_receiveArgs);
            TryDispose(_sendArgs);
        }

        private static void TryDispose(IDisposable disposable)
        {
            try
            {
                disposable.Dispose();
            }
// ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }
        }
    }
}