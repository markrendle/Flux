namespace Flux
{
    using System;
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using App = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Collections.Generic.IDictionary<string, string[]>, System.IO.Stream, System.Threading.CancellationToken, System.Func<int, System.Collections.Generic.IDictionary<string, string[]>, System.Func<System.IO.Stream, System.Threading.CancellationToken, System.Threading.Tasks.Task>, System.Threading.Tasks.Task>, System.Delegate, System.Threading.Tasks.Task>;
    using BodyDelegate = System.Func<System.IO.Stream, System.Threading.CancellationToken, System.Threading.Tasks.Task>;

    internal sealed class Instance : IDisposable
    {
        private readonly byte[] _100Continue = Encoding.UTF8.GetBytes("HTTP/1.1 100 Continue\r\n");
        private readonly Socket _socket;
        private readonly App _app;
        private NetworkStream _networkStream;

        public Instance(Socket socket, App app)
        {
            _socket = socket;
            _app = app;
        }

        public Task Run()
        {
            var env = new Dictionary<string, object>
                {
                    { OwinConstants.Version, "0.8" }
                };
            _networkStream = new NetworkStream(_socket, false);
            var requestLine = RequestLineParser.Parse(_networkStream);
            env[OwinConstants.RequestMethod] = requestLine.Method;
            if (requestLine.Uri.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
            {
                Uri uri;
                if (Uri.TryCreate(requestLine.Uri, UriKind.Absolute, out uri))
                {
                    env[OwinConstants.RequestPath] = uri.AbsolutePath;
                    env[OwinConstants.RequestQueryString] = uri.Query;
                    env[OwinConstants.RequestScheme] = uri.Scheme;
                }
            }
            var headers = HeaderParser.Parse(_networkStream);
            string[] expectContinue;
            if (headers.TryGetValue("Expect", out expectContinue))
            {
                if (expectContinue.Length == 1 && expectContinue[0].Equals("100-Continue", StringComparison.OrdinalIgnoreCase))
                {
                    _networkStream.Write(_100Continue, 0, _100Continue.Length);
                }
            }
            return _app(env, headers, _networkStream, CancellationToken.None, Result, null);
        }

        private Task Result(int status, IDictionary<string, string[]> headers, BodyDelegate body)
        {
            var headerBuilder = new StringBuilder("HTTP/1.1 " + status + "\r\n");
            foreach (var header in headers)
            {
                foreach (var value in header.Value)
                {
                    headerBuilder.AppendFormat("{0}: {1}\r\n", header.Key, value);
                }
            }
            headerBuilder.Append("\r\n");
            var bytes = Encoding.UTF8.GetBytes(headerBuilder.ToString());
            return _networkStream.WriteAsync(bytes, 0, bytes.Length)
                .ContinueWith(t =>
                    {
                        if (!(t.IsFaulted || t.IsCanceled))
                        {
                            return body(_networkStream, CancellationToken.None);
                        }
                        return t;
                    }).Unwrap();
        }

        public void Dispose()
        {
            _socket.Close();
        }
    }
}