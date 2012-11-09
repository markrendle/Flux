namespace Flux
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Fix;
    using AppFunc = System.Func< // Call
        System.Collections.Generic.IDictionary<string, object>, // Environment
        System.Threading.Tasks.Task>; // Completion

    internal sealed class Instance : IDisposable
    {
        private readonly byte[] _100Continue = Encoding.UTF8.GetBytes("HTTP/1.1 100 Continue\r\n");
        private readonly TcpClient _tcpClient;
        private readonly AppFunc _app;
        private readonly NetworkStream _networkStream;
        private readonly byte[] _404NotFound = Encoding.UTF8.GetBytes("HTTP/1.1 404 Not found");
        private readonly byte[] _500InternalServerError = Encoding.UTF8.GetBytes("HTTP/1.1 500 Internal Server Error");
        private BufferStream _bufferStream;

        public Instance(TcpClient tcpClient, AppFunc app)
        {
            _tcpClient = tcpClient;
            _networkStream = _tcpClient.GetStream();
            _app = app;
        }

        public Task Run()
        {
            try
            {
                var env = CreateEnvironmentDictionary();
                var headers = HeaderParser.Parse(_networkStream);
                env[OwinKeys.RequestHeaders] = headers;
                env[OwinKeys.ResponseHeaders] = new Dictionary<string, string[]>();
                env[OwinKeys.ResponseBody] = Buffer;
                string[] expectContinue;
                if (headers.TryGetValue("Expect", out expectContinue))
                {
                    if (expectContinue.Length == 1 && expectContinue[0].Equals("100-Continue", StringComparison.OrdinalIgnoreCase))
                    {
                        return _networkStream.WriteAsync(_100Continue, 0, _100Continue.Length)
                            .ContinueWith(t =>
                                {
                                    if (t.IsFaulted) return t;
                                    return _app(env)
                                        .ContinueWith(t2 => Result(t2, env));
                                }).Unwrap();
                    }
                }
                return _app(env)
                    .ContinueWith(t2 => Result(t2, env));
            }
            catch (Exception ex)
            {
                var tcs = new TaskCompletionSource<object>();
                tcs.SetException(ex);
                return tcs.Task;
            }
        }

        private Dictionary<string, object> CreateEnvironmentDictionary()
        {
            var env = new Dictionary<string, object>
                          {
                              {OwinKeys.Version, "0.8"}
                          };
            var requestLine = RequestLineParser.Parse(_networkStream);
            env[OwinKeys.RequestMethod] = requestLine.Method;
            env[OwinKeys.RequestPathBase] = string.Empty;
            if (requestLine.Uri.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
            {
                Uri uri;
                if (Uri.TryCreate(requestLine.Uri, UriKind.Absolute, out uri))
                {
                    env[OwinKeys.RequestPath] = uri.AbsolutePath;
                    env[OwinKeys.RequestQueryString] = uri.Query;
                    env[OwinKeys.RequestScheme] = uri.Scheme;
                }
            }
            else
            {
                var splitUri = requestLine.Uri.Split('?');
                env[OwinKeys.RequestPath] = splitUri[0];
                env[OwinKeys.RequestQueryString] = splitUri.Length == 2 ? splitUri[1] : string.Empty;
                env[OwinKeys.RequestScheme] = "http";
            }

            env[OwinKeys.RequestBody] = _networkStream;
            env[OwinKeys.CallCancelled] = new CancellationToken();
            return env;
        }

        internal BufferStream Buffer
        {
            get { return _bufferStream ?? (_bufferStream = new BufferStream());}
        }

        private Task Result(Task task, IDictionary<string,object> env)
        {
            if (task.IsFaulted)
            {
                return _networkStream.WriteAsync(_500InternalServerError, 0, _500InternalServerError.Length)
                    .ContinueWith(_ => Dispose());
            }

            int status = env.GetValueOrDefault(OwinKeys.ResponseStatusCode, 0);
            if (status == 0 || status == 404)
            {
                return _networkStream.WriteAsync(_404NotFound, 0, _404NotFound.Length)
                    .ContinueWith(_ => Dispose());
            }

            return WriteResult(status, env);
        }

        private Task WriteResult(int status, IDictionary<string,object> env)
        {
            var headerBuilder = new StringBuilder("HTTP/1.1 " + status + "\r\n");

            var headers = (IDictionary<string,string[]>)env[OwinKeys.ResponseHeaders];
            if (!headers.ContainsKey("Content-Length"))
            {
                headers["Content-Length"] = new[] {Buffer.Length.ToString(CultureInfo.InvariantCulture)};
            }
            foreach (var header in headers)
            {
                switch (header.Value.Length)
                {
                    case 0:
                        continue;
                    case 1:
                        headerBuilder.AppendFormat("{0}: {1}\r\n", header.Key, header.Value[0]);
                        continue;
                }
                foreach (var value in header.Value)
                {
                    headerBuilder.AppendFormat("{0}: {1}\r\n", header.Key, value);
                }
            }

            return WriteResponse(headerBuilder);
        }

        private Task WriteResponse(StringBuilder headerBuilder)
        {
            headerBuilder.Append("\r\n");
            var bytes = Encoding.UTF8.GetBytes(headerBuilder.ToString());

            var task = _networkStream.WriteAsync(bytes, 0, bytes.Length);
            if (task.IsFaulted || task.IsCanceled) return task;
            if (Buffer.Length > 0)
            {
                task = task.ContinueWith(t => WriteBuffer()).Unwrap();
            }

            return task;
        }

        private Task WriteBuffer()
        {
            if (Buffer.Length <= int.MaxValue)
            {
                Buffer.Position = 0;
                byte[] buffer;
                if (Buffer.TryGetBuffer(out buffer))
                {
                    return _networkStream.WriteAsync(buffer, 0, (int)Buffer.Length);
                }
                Buffer.CopyTo(_networkStream);
            }
            return TaskHelper.Completed();
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