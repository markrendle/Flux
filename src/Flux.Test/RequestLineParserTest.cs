namespace Flux.Test
{
    using System.IO;
    using System.Text;
    using Xunit;

    public class RequestLineParserTest
    {
        [Fact]
        public void ParsesRequestLine()
        {
            const string method = "GET";
            const string uri = "/foo/bar";
            const string version = "HTTP/1.1";

            var bytes = Encoding.Default.GetBytes(string.Format("{0} {1} {2}", method, uri, version));
            var stream = new MemoryStream(bytes);
            var requestLine = RequestLineParser.Parse(stream);

            Assert.Equal(method, requestLine.Method);
            Assert.Equal(uri, requestLine.Uri);
            Assert.Equal(version, requestLine.HttpVersion);
        }
    }
}