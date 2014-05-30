namespace Flux.Owin
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Text;

    public partial class FluxEnvironment
    {
        private const byte Space = (byte)' ';
        private const byte QuestionMark = (byte)'?';
        private const byte Slash = (byte)'/';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ParseKey(string key)
        {
            switch (key)
            {
                case OwinKeys.RequestMethod:
                case OwinKeys.RequestPath:
                case OwinKeys.RequestQueryString:
                case OwinKeys.RequestProtocol:
                    ParseRequestLine();
                    break;
                default:
                    throw new KeyNotFoundException();
            }
        }

        private void ParseRequestLine()
        {
            lock (_syncRoot)
            {
                if (_internal.ContainsKey(OwinKeys.RequestMethod)) return;
                _internal.Add(OwinKeys.RequestPathBase, string.Empty);

                var data = _data[0];

                int nextSpace = NextSpace(data, data.Offset);
                _internal.Add(OwinKeys.RequestMethod, Encoding.UTF8.GetString(data.Array, data.Offset, nextSpace - data.Offset));
                int start = nextSpace + 1;
                nextSpace = NextSpace(data, start);
                int questionMark = Array.IndexOf(data.Array, QuestionMark, start, _requestLineCount);
                if (questionMark > 0)
                {
                    _internal.Add(OwinKeys.RequestPath, Encoding.UTF8.GetString(data.Array, start, questionMark - start));
                    _internal.Add(OwinKeys.RequestQueryString, Encoding.UTF8.GetString(data.Array, questionMark, nextSpace - questionMark));
                }
                else
                {
                    _internal.Add(OwinKeys.RequestPath, Encoding.UTF8.GetString(data.Array, start, nextSpace - start));
                    _internal.Add(OwinKeys.RequestQueryString, string.Empty);
                }
                start = nextSpace + 1;
                int newline = NextNewline(data, start);
                _internal.Add(OwinKeys.RequestProtocol, Encoding.UTF8.GetString(data.Array, start, newline - start));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int NextSpace(ArraySegment<byte> data, int startIndex)
        {
            return Array.IndexOf(data.Array, Space, startIndex, data.Count - startIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int NextNewline(ArraySegment<byte> data, int startIndex)
        {
            return Array.IndexOf(data.Array, NewLine, startIndex, data.Count - startIndex);
        }
    }
}