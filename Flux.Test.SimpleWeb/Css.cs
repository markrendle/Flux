namespace Flux.Test.SimpleWeb
{
    using System.IO;
    using System.Text;
    using Simple.Web;
    using Simple.Web.Behaviors;

    [UriTemplate("/css/site.css")]
    public class Css : IGet, IOutputStream
    {
        private static readonly byte[] Buffer = Encoding.UTF8.GetBytes("h1 { font-family: 'Segoe UI'; }");
        public Status Get()
        {
            return 200;
        }

        public string ContentType {
            get { return "text/css"; }
        }

        public string ContentDisposition { get; private set; }

        public Stream Output
        {
            get { return new MemoryStream(Buffer); }
        }
    }
}