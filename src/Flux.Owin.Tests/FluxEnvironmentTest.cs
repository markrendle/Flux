using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Flux.Owin.Tests
{
    public class FluxEnvironmentTest
    {
        private static readonly Socket DummySocket = new Socket(SocketType.Raw, ProtocolType.Tcp);
        private static readonly byte[] SimpleRequest = Encoding.UTF8.GetBytes("GET / HTTP/1.1\nHost: localhost\n\n");
        private static readonly byte[] LessSimpleRequest = Encoding.UTF8.GetBytes("GET /home?r=1 HTTP/1.1\nHost: localhost\n\n");

        [Fact]
        public void GetsMethod()
        {
            var env = new FluxEnvironment(DummySocket, SimpleRequest, 0, SimpleRequest.Length - 1);
            Assert.Equal("GET", env[OwinKeys.RequestMethod]);
        }

        [Fact]
        public void GetsPathForPlainRequest()
        {
            var env = new FluxEnvironment(DummySocket, SimpleRequest, 0, SimpleRequest.Length - 1);
            Assert.Equal("/", env[OwinKeys.RequestPath]);
        }

        [Fact]
        public void GetsPathForRequestWithQueryString()
        {
            var env = new FluxEnvironment(DummySocket, LessSimpleRequest, 0, LessSimpleRequest.Length - 1);
            Assert.Equal("/home", env[OwinKeys.RequestPath]);
        }
    }
}
