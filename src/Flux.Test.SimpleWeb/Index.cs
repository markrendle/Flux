namespace Flux.Test.SimpleWeb
{
    using Simple.Web;
    using Simple.Web.Behaviors;

    [UriTemplate("/")]
    public class Index : IGet, IOutput<RawHtml>
    {
        public const string Html = "<html><head><link href=\"css/site.css\" rel=\"stylesheet\" type=\"text/css\"></head><body><h1>Pass</h1></body></html>";

        public Status Get()
        {
            return 200;
        }

        public RawHtml Output { get { return Html; } }
    }
}