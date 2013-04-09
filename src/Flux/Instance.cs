namespace Flux
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using App = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Collections.Generic.IDictionary<string, string[]>, System.IO.Stream, System.Threading.CancellationToken, System.Func<int, System.Collections.Generic.IDictionary<string, string[]>, System.Func<System.IO.Stream, System.Threading.CancellationToken, System.Threading.Tasks.Task>, System.Threading.Tasks.Task>, System.Delegate, System.Threading.Tasks.Task>;
    using BodyDelegate = System.Func<System.IO.Stream, System.Threading.CancellationToken, System.Threading.Tasks.Task>;

    internal sealed class Instance : IDisposable
    {
        private readonly byte[] _100Continue = Encoding.UTF8.GetBytes("HTTP/1.1 100 Continue\r\n");
        private readonly TcpClient _tcpClient;
        private readonly App _app;
        private readonly NetworkStream _networkStream;
        private readonly byte[] _404NotFound = Encoding.UTF8.GetBytes("HTTP/1.1 404 Not found");
        private BufferStream _bufferStream;

        public Instance(TcpClient tcpClient, App app)
        {
            _tcpClient = tcpClient;
            _networkStream = _tcpClient.GetStream();
            _app = app;
        }

        public Task Run()
        {
            try
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
            catch (Exception ex)
            {
                var tcs = new TaskCompletionSource<object>();
                tcs.SetException(ex);
                return tcs.Task;
            }
        }

        internal BufferStream Buffer
        {
            get { return _bufferStream ?? (_bufferStream = new BufferStream());}
        }

        private Task Result(int status, IDictionary<string, string[]> headers, BodyDelegate body)
        {
            if (status == 0)
            {
                return _networkStream.WriteAsync(_404NotFound, 0, _404NotFound.Length)
                    .ContinueWith(_ => Dispose());
            }

            bool contentLengthSet = false;

            var headerBuilder = new StringBuilder("HTTP/1.1 " + status + "\r\n");
            foreach (var header in headers)
            {
                if (header.Key.Equals("Content-Length", StringComparison.InvariantCultureIgnoreCase))
                {
                    contentLengthSet = true;
                }
                foreach (var value in header.Value)
                {
                    headerBuilder.AppendFormat("{0}: {1}\r\n", header.Key, value);
                }
            }

            if (body == null)
            {
                headerBuilder.Append("\r\n");
                var bytes = Encoding.UTF8.GetBytes(headerBuilder.ToString());
                return _networkStream.WriteAsync(bytes, 0, bytes.Length)
                    .ContinueWith(t => Dispose());
            }

            if (!contentLengthSet)
            {
                return WriteWithBuffer(body, headerBuilder);
            }

            return WriteUnbuffered(body, headerBuilder);
        }

        private Task WriteUnbuffered(BodyDelegate body, StringBuilder headerBuilder)
        {
            headerBuilder.Append("\r\n");
            var bytes = Encoding.UTF8.GetBytes(headerBuilder.ToString());
            return _networkStream.WriteAsync(bytes, 0, bytes.Length)
                .ContinueWith(t =>
                    {
                        if (t.IsFaulted || t.IsCanceled)
                        {
                            return t;
                        }
                        return body(_networkStream, CancellationToken.None)
                            .ContinueWith(_ => Dispose());
                    }).Unwrap();
        }

        private Task WriteWithBuffer(BodyDelegate body, StringBuilder headerBuilder)
        {
            return body(Buffer, CancellationToken.None)
                .ContinueWith(t =>
                    {
                        Buffer.InternalStream.Position = 0;
                        headerBuilder.AppendFormat("Content-Length: {0}\r\n", Buffer.InternalStream.Length);
                        headerBuilder.Append("\r\n");
                        var bytes = Encoding.UTF8.GetBytes(headerBuilder.ToString());
                        if (!(t.IsFaulted || t.IsCanceled))
                        {
                            var writeTask = _networkStream.WriteAsync(bytes, 0, bytes.Length);
                            if (Buffer.InternalStream.Length > 0)
                            {
                                return CopyBufferToNetworkStream(writeTask);
                            }
                            return writeTask;
                        }
                        Buffer.ForceDispose();
                        Dispose();
                        return t;
                    }).Unwrap();
        }

        private Task CopyBufferToNetworkStream(Task writeTask)
        {
            return writeTask.ContinueWith(t2 =>
                {
                    if (!(t2.IsFaulted || t2.IsCanceled))
                    {
                        byte[] buffer;
                        if (Buffer.Length < int.MaxValue && Buffer.TryGetBuffer(out buffer))
                        {
                            return _networkStream.WriteAsync(buffer, 0, (int) Buffer.Length)
                                .ContinueWith(t3 => Dispose());
                        }
                        Buffer.InternalStream.CopyTo(_networkStream);
                    }
                    Dispose();
                    return writeTask;
                }).Unwrap();
        }

        public void Dispose()
        {
            try
            {
                _tcpClient.Close();
            }
            catch(Exception ex)
            {
                Trace.TraceError(ex.Message);
            }
            if (_bufferStream != null)
            {
                try
                {
                    _bufferStream.ForceDispose();
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.Message);
                }
            }
        }
    }
}