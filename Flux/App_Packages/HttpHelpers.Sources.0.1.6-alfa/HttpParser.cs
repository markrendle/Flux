#region License
//
// Http Helpers Library: HttpParser.cs
//
// Author:
//   Giacomo Stelluti Scala (gsscoder@gmail.com)
//
// Copyright (C) 2013 Giacomo Stelluti Scala
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
#endregion
#region Preprocessor Directives
// When using source distribution, uncomment the following line to
// disable async API
#define NO_ASYNC_API
#endregion
#region Using Directives
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
#endregion

namespace HttpHelpers
{
    public static partial class HttpParser
    {
#if !NO_ASYNC_API
        public static async Task<bool> ParseMessageAsync(Stream stream,
            Action<string, string, string> onHeadingLine,
            Action<string, string> onHeaderLine)
        {
            // why Debug.Assert? in production code there's no excuse to pass null delegates
            Debug.Assert(onHeadingLine != null);
            Debug.Assert(onHeaderLine != null);

            if (stream == null) { throw new ArgumentNullException("stream"); }

            var reader = new StreamReader(stream);

            var headingParsing = await ParseHeadingLineAsync(reader, onHeadingLine);
            if (!headingParsing)
            {
                return false;
            }

            while (true)
            {
                var headerParsing = await ParseHeaderLineAsync(reader, onHeaderLine);
                if (headerParsing == null)
                {
                    break;
                }
                if (!headerParsing.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private static async Task<bool> ParseHeadingLineAsync(StreamReader reader,
            Action<string, string, string> onHeadingLine)
        {
            var line = await reader.ReadLineAsync();

            return RawParser.ParseHeadingLine(line, onHeadingLine);
        }

        private static async Task<bool?> ParseHeaderLineAsync(StreamReader reader,
            Action<string, string> onHeaderLine)
        {
            var line = await reader.ReadLineAsync();
            if (line.Length == 0)
            {
                return null;
            }

            return RawParser.ParseHeaderLine(line, onHeaderLine);
        }
#endif

        public static bool ParseMessage(Stream stream,
            Action<string, string, string> onHeadingLine,
            Action<string, string> onHeaderLine)
        {
            // why Debug.Assert? in production code there's no excuse to pass null delegates
            Debug.Assert(onHeadingLine != null);
            Debug.Assert(onHeaderLine != null);

            if (stream == null) { throw new ArgumentNullException("stream"); }

            var reader = new StreamReader(stream);

            var headingParsing = RawParser.ParseHeadingLine(reader.ReadLine(), onHeadingLine);
            if (!headingParsing)
            {
                return false;
            }

            while (true)
            {
                var headerLine = reader.ReadLine();
                headerLine = headerLine ?? string.Empty;
                if (headerLine.Length == 0)
                {
                    break;
                }
                var headerParsing = RawParser.ParseHeaderLine(headerLine, onHeaderLine);
                if (!headerParsing)
                {
                    return false;
                }
            }

            return true;
        }
    }
}