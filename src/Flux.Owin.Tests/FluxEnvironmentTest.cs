using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Flux.Owin.Tests
{
    using System.Threading;

    public class FluxEnvironmentTest
    {
        private static readonly Socket DummySocket = new Socket(SocketType.Raw, ProtocolType.Tcp);
        private static readonly byte[] SimpleRequest = Encoding.UTF8.GetBytes("GET / HTTP/1.1\nHost: localhost\n\n");
        private static readonly byte[] LessSimpleRequest = Encoding.UTF8.GetBytes("GET /home?r=1 HTTP/1.1\nHost: localhost\n\n");

        [Fact]
        public void GetsMethod()
        {
            var env = CreateEnv(SimpleRequest);
            Assert.Equal("GET", env[OwinKeys.RequestMethod]);
        }

        private static FluxEnvironment CreateEnv(byte[] request)
        {
            var segment = new ArraySegment<byte>(request, 0, request.Length);
            return new FluxEnvironment(DummySocket, segment, RequestScheme.Http, CancellationToken.None);
        }

        [Fact]
        public void GetsPathForPlainRequest()
        {
            var env = CreateEnv(SimpleRequest);
            Assert.Equal("/", env[OwinKeys.RequestPath]);
        }

        [Fact]
        public void GetsPathForRequestWithQueryString()
        {
            var env = CreateEnv(LessSimpleRequest);
            Assert.Equal("/home", env[OwinKeys.RequestPath]);
        }

        [Fact]
        public void GetsProtocolForPlainRequest()
        {
            var env = CreateEnv(SimpleRequest);
            Assert.Equal("HTTP/1.1", env[OwinKeys.RequestProtocol]);
        }

        [Fact]
        public void GetsProtocolForRequestWithQueryString()
        {
            var env = CreateEnv(LessSimpleRequest);
            Assert.Equal("HTTP/1.1", env[OwinKeys.RequestProtocol]);
        }
    }
}
