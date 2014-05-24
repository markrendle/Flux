namespace Flux
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public static class OwinKeys
    {
        public const string CallCompleted = "owin.CallCompleted";
        public const string RequestScheme = "owin.RequestScheme";
        public const string RequestMethod = "owin.RequestMethod";
        public const string RequestPathBase = "owin.RequestPathBase";
        public const string RequestPath = "owin.RequestPath";
        public const string RequestQueryString = "owin.RequestQueryString";
        public const string RequestProtocol = "owin.RequestProtocol";

        public const string RequestHeaders = "owin.RequestHeaders";
        public const string RequestBody = "owin.RequestBody";

        public const string ResponseBody = "owin.ResponseBody";
        public const string ResponseHeaders = "owin.ResponseHeaders";
        public const string ResponseStatusCode = "owin.ResponseStatusCode";
        public const string ResponseReasonPhrase = "owin.ResponseReasonPhrase";
        public const string ResponseProtocol = "owin.ResponseProtocol";

        public const string Version = "owin.Version";
        public const string CallCancelled = "owin.CallCancelled";

        private static readonly Lazy<HashSet<string>> LazyKeys = new Lazy<HashSet<string>>(CreateKeySet);

        public static HashSet<string> Keys
        {
            get { return LazyKeys.Value; }
        }

        private static HashSet<string> CreateKeySet()
        {
            return
                new HashSet<string>(
                    typeof (OwinKeys).GetFields(BindingFlags.Public | BindingFlags.Static)
                        .Select(f => (string) f.GetValue(null)));
        }
    }

    public static class ServerKeys
    {
        public const string RemoteIpAddress = "server.RemoteIpAddress";
        public const string RemotePort = "server.RemotePort";
        public const string LocalIpAddress = "server.LocalIpAddress";
        public const string LocalPort = "server.LocalPort";
        public const string IsLocal = "server.IsLocal";
    }
}