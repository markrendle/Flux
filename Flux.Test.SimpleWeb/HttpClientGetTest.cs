using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flux.Test.SimpleWeb
{
    using System.Net;
    using Fix;
    using Simple.Web;
    using Simple.Web.Behaviors;
    using Xunit;

    public class WebClientGetTest
    {
        [Fact]
        public void SimpleGet()
        {
            var server = new Server(Application.Run, 3001);
            server.Start();

            string actual;
            using (var client = new WebClient())
            {
                actual = client.DownloadString("http://localhost:3001/");
            }

            server.Stop();
            Assert.Equal("<h1>Pass</h1>", actual);
        }
    }

    [UriTemplate("/")]
    public class Index : IGet, IOutput<RawHtml>
    {
        public Status Get()
        {
            return 200;
        }

        public RawHtml Output { get { return "<h1>Pass</h1>"; } }
    }
}
