﻿using System;
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
        private const byte Slash = (byte) '/';

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

                int nextSpace = NextSpace(_data.Offset);
                _internal.Add(OwinKeys.RequestMethod, Encoding.UTF8.GetString(_data.Array, _data.Offset, nextSpace - _data.Offset));
                int start = nextSpace + 1;
                nextSpace = NextSpace(start);
                int questionMark = Array.IndexOf(_data.Array, QuestionMark, start, _requestLineCount);
                if (questionMark > 0)
                {
                    _internal.Add(OwinKeys.RequestPath, Encoding.UTF8.GetString(_data.Array, start, questionMark - start));
                    _internal.Add(OwinKeys.RequestQueryString, Encoding.UTF8.GetString(_data.Array, questionMark, nextSpace - questionMark));
                }
                else
                {
                    _internal.Add(OwinKeys.RequestPath, Encoding.UTF8.GetString(_data.Array, start, nextSpace - start));
                    _internal.Add(OwinKeys.RequestQueryString, string.Empty);
                }
                start = nextSpace + 1;
                int newline = NextNewline(start);
                _internal.Add(OwinKeys.RequestProtocol, Encoding.UTF8.GetString(_data.Array, start, newline - start));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int NextSpace(int startIndex)
        {
            return Array.IndexOf(_data.Array, Space, startIndex, _data.Count - startIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int NextNewline(int startIndex)
        {
            return Array.IndexOf(_data.Array, NewLine, startIndex, _data.Count - startIndex);
        }
    }
}