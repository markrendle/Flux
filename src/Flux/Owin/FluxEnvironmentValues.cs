using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Flux.Owin
{
    public partial class FluxEnvironment
    {
        private const byte Space = (byte) ' ';
        private const byte QuestionMark = (byte) '?';
        private string _requestScheme;
        private string _requestMethod;
        private string _requestPathBase;
        private string _requestPath;
        private string _requestQueryString;
        private string _requestProtocol;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object Get(string key)
        {
            switch (key)
            {
                case OwinKeys.RequestMethod:
                    return _requestMethod ?? GetMethod();
                case OwinKeys.RequestPath:
                    return _requestPath ?? GetPath();
                default:
                    throw new KeyNotFoundException();
            }
        }

        private string GetPath()
        {
            int start = Array.IndexOf(_buffer, Space, _offset) + 1;
            int end = Array.IndexOf(_buffer, QuestionMark, start, _requestLineCount);
            if (end < 0)
            {
                end = Array.IndexOf(_buffer, Space, start, _requestLineCount);
            }

            return _requestPath = Encoding.UTF8.GetString(_buffer, start, end - start);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetMethod()
        {
            int space = Array.IndexOf(_buffer, Space, _offset);
            return _requestMethod = Encoding.UTF8.GetString(_buffer, _offset, space - _offset);
        }
    }
}
