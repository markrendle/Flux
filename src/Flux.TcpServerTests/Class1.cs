using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flux.TcpServerTests
{
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Threading;
    using Xunit;

    public class Class1
    {
        [Fact]
        public void Test1()
        {
            bool done = false;
            HttpStatusCode statusCode = default(HttpStatusCode);
            var tcpServer = new FluxServer(new IPAddress(new byte[]{127,0,0,1}), 13589);
            tcpServer.Start(async env =>
            {
                env[OwinKeys.ResponseStatusCode] = 200;
                done = true;
            });
            try
            {
                using (var http = new HttpClient())
                {
                    Func<Task> get = async () =>
                    {
                        var response =
                            await http.GetAsync("http://127.0.0.1:13589/", HttpCompletionOption.ResponseHeadersRead);
                        statusCode = response.StatusCode;
                    };
                    get().Wait(1000);
                }
                tcpServer.Stop();
            }
            catch (Exception ex)
            {
                tcpServer.Stop();
                Trace.TraceError(ex.ToString());
            }
            Assert.True(done);
            Assert.Equal(HttpStatusCode.OK, statusCode);
        }
    }

    class Handler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    static class Shared
    {
        public static readonly DataPool Data = new DataPool();
    }
}
