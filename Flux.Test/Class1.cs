namespace Flux.Test
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Xunit;

    public class GetRequestParseTest
    {
        [Fact]
        public void ParsesRequestLineAndHeadersFromStream()
        {
            const string method = "GET";
            const string uri = "/foo/bar";
            const string version = "HTTP/1.1";
            var stream = new MemoryStream(Encoding.Default.GetBytes(Properties.Resources.RequestSample));
            var requestLine = RequestLineParser.Parse(stream);
            Assert.Equal(method, requestLine.Method);
            Assert.Equal(uri, requestLine.Uri);
            Assert.Equal(version, requestLine.HttpVersion);
            var headers = HeaderParser.Parse(stream);
            Assert.Equal(headers["Content-Length"].Single(), "8");
            Assert.Equal(headers["Proxy-Connection"].Single(), "Keep-Alive");
            Assert.Equal(headers["Transfer-Encoding"].Single(), "chunked");
            Assert.Equal(headers["Via"].Single(), "1.1 TK5-PRXY-21");
            Assert.Equal(headers["Expires"].Single(), "Thu, 19 Nov 1981 08:52:00 GMT");
            Assert.Equal(headers["Date"].Single(), "Mon, 16 Jan 2012 23:39:47 GMT");
            Assert.Equal(headers["Server"].Single(), "nginx/0.6.30");
            Assert.Equal(headers["X-Powered-By"].Single(), "PHP/5.2.4-2ubuntu5.6");
            Assert.Equal(headers["Pragma"].Single(), "no-cache");
            Assert.Equal(headers["X-Pingback"].Single(), "http://whereslou.com/xmlrpc.php");
            Assert.Equal(headers["Host"].Single(), "whereslou.com");
            Assert.Equal(headers["User-Agent"].Single(), "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:8.0) Gecko/20100101 Firefox/8.0");
            Assert.Equal(headers["Accept"].Single(), "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            Assert.Equal(headers["Accept-Language"].Single(), "en-us,en;q=0.5");
            Assert.Equal(headers["Accept-Encoding"].Single(), "gzip, deflate");
            Assert.Equal(headers["Accept-Charset"].Single(), "ISO-8859-1,utf-8;q=0.7,*;q=0.7");
            Assert.Equal(headers["Cookie"].Single(), "PHPSESSID=a3047ee50d8ba3f4302a9926aasdf; wordpress_test_cookie=WP+Cookie+check; wp-settings-1=editor%3Dhtml%26m0%3Do%26m1%3Do%26m2%3Dc%26m3%3Dc%26m4%3Dc%26m5%3Do%26m6%3Do%26m7%3Do%26m8%3Dc%26m9%3Dc%26m10%3Dc%26imgsize%3Dmedium%26urlbutton%3Durlfile%26align%3Dright; wp-settings-time-1=1326754593; __utma=24333308.2009914498.1326754717.1326754717.1326754717.1; __utmc=24333308; __utmz=24333308.1326754717.1.1.utmcsr=(direct)|utmccn=(direct)|utmcmd=(none)");

            Assert.Contains("max-age=0", headers["Cache-Control"]);
            Assert.Contains("no-store, no-cache, must-revalidate, post-check=0, pre-check=0", headers["Cache-Control"]);

            Assert.Contains("text/html", headers["Content-Type"]);
            Assert.Contains("text/html; charset=UTF-8", headers["Content-Type"]);
            Assert.Contains("keep-alive", headers["Connection"]);
            Assert.Contains("Keep-Alive", headers["Connection"]);
        }

        private static void AssertMulti(IEnumerable<string> actual, params string[] values)
        {
            var list = actual.ToList();
            foreach (var value in values)
            {
                Assert.Contains(value, list);
            }
        }
    }
}
