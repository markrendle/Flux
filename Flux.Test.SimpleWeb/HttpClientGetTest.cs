using System;

namespace Flux.Test.SimpleWeb
{
    using System.Collections.Concurrent;
    using System.Net;
    using System.Threading;
    using Simple.Web;
    using Simple.Web.Behaviors;
    using Xunit;

    public class WebClientGetTest
    {
        [Fact]
        public void SimpleGet()
        {
            var server = new Server(3001);
            server.Start(Application.Run);

            string actual;
            using (var client = new WebClient())
            {
                actual = client.DownloadString("http://localhost:3001/");
            }

            server.Stop();
            Assert.Equal("<h1>Pass</h1>", actual);
        }

        [Fact]
        public void TenConcurrentSimpleGet()
        {
            var server = new Server(3002);
            server.Start(Application.Run);
            var uri = new Uri("http://localhost:3002/");
            var bag = new ConcurrentBag<string>();
            const int count = 10;
            for (int i = 0; i < count; i++)
            {
                var client = new WebClient();
                client.DownloadStringCompleted += (sender, args) =>
                    {
                        bag.Add(args.Result);
                        client.Dispose();
                    };
                client.DownloadStringAsync(uri);
            }
            SpinWait.SpinUntil(() => bag.Count == count, 10000);
            server.Stop();
            Assert.Equal(count, bag.Count);
            foreach (var actual in bag)
            {
                Assert.Equal("<h1>Pass</h1>", actual);
            }
        }
        
        [Fact]
        public void OneHundredConcurrentSimpleGet()
        {
            var server = new Server(3003);
            server.Start(Application.Run);
            var uri = new Uri("http://localhost:3003/");
            var bag = new ConcurrentBag<string>();
            const int count = 100;
            for (int i = 0; i < count; i++)
            {
                var client = new WebClient();
                client.DownloadStringCompleted += (sender, args) =>
                    {
                        bag.Add(args.Result);
                        client.Dispose();
                    };
                client.DownloadStringAsync(uri);
            }
            SpinWait.SpinUntil(() => bag.Count == count, 10000);
            server.Stop();
            Assert.Equal(count, bag.Count);
            foreach (var actual in bag)
            {
                Assert.Equal("<h1>Pass</h1>", actual);
            }
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
