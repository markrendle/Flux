namespace Flux
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using App = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Collections.Generic.IDictionary<string, string[]>, System.IO.Stream, System.Threading.CancellationToken, System.Func<int, System.Collections.Generic.IDictionary<string, string[]>, System.Func<System.IO.Stream, System.Threading.CancellationToken, System.Threading.Tasks.Task>, System.Threading.Tasks.Task>, System.Delegate, System.Threading.Tasks.Task>;
    using BodyDelegate = System.Func<System.IO.Stream, System.Threading.CancellationToken, System.Threading.Tasks.Task>;

    internal sealed class Instance : IDisposable
    {
        private static readonly ConcurrentDictionary<Socket, int> LiveSockets = new ConcurrentDictionary<Socket, int>();
        private readonly byte[] _100Continue = Encoding.UTF8.GetBytes("HTTP/1.1 100 Continue\r\n");
        private readonly TcpClient _tcpClient;
        private readonly App _app;
        private readonly NetworkStream _networkStream;

        public Instance(TcpClient tcpClient, App app)
        {
            _tcpClient = tcpClient;
            _networkStream = _tcpClient.GetStream();
            _app = app;
        }

        public Task Run()
        {
            var env = new Dictionary<string, object>
                {
                    { OwinConstants.Version, "0.8" }
                };
            var requestLine = RequestLineParser.Parse(_networkStream);
            env[OwinConstants.RequestMethod] = requestLine.Method;
            env[OwinConstants.RequestPathBase] = string.Empty;
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
            else
            {
                var splitUri = requestLine.Uri.Split('?');
                env[OwinConstants.RequestPath] = splitUri[0];
                env[OwinConstants.RequestQueryString] = splitUri.Length == 2 ? splitUri[1] : string.Empty;
                env[OwinConstants.RequestScheme] = "http";
            }
            var headers = HeaderParser.Parse(_networkStream);
            string[] expectContinue;
            if (headers.TryGetValue("Expect", out expectContinue))
            {
                if (expectContinue.Length == 1 && expectContinue[0].Equals("100-Continue", StringComparison.OrdinalIgnoreCase))
                {
                    return _networkStream.WriteAsync(_100Continue, 0, _100Continue.Length)
                        .ContinueWith(t =>
                            {
                                if (t.IsFaulted) return t;
                                return _app(env, headers, _networkStream, CancellationToken.None, Result, null);
                            }).Unwrap();
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
                            return body(_networkStream, CancellationToken.None)
                                .ContinueWith(_ => Dispose());
                        }
                        return t;
                    }).Unwrap();
        }

        public void Dispose()
        {
            _tcpClient.Close();
        }
    }
}