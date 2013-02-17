#region License
//
// Http Helpers Library: HttpParser.RawParser.cs
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
#region Using Directives
using System;
using System.IO;
using HttpHelpers.Extensions;
#endregion

namespace HttpHelpers
{
    partial class HttpParser
    {
        private static class RawParser 
        {
            public static bool ParseHeadingLine(string line, Action<string, string, string> onHeadingLine)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    return false;
                }

                var stringReader = new StringReader(line);

                var item1 = stringReader.TakeWhile(c => !c.IsWhiteSpace());
                if (!stringReader.PeekChar().IsWhiteSpace())
                {
                    return false;
                }
                stringReader.SkipWhiteSpace();

                var item2 = stringReader.TakeWhile(c => !c.IsWhiteSpace());
                if (!stringReader.PeekChar().IsWhiteSpace())
                {
                    return false;
                }
                stringReader.SkipWhiteSpace();

                var item3 = stringReader.ReadToEnd();

                onHeadingLine(item1, item2, item3);

                return true;
            }

            public static bool ParseHeaderLine(string line, Action<string, string> onHeaderLine)
            {
                var stringReader = new StringReader(line);

                stringReader.SkipWhiteSpace();

                var header = stringReader.TakeWhile(c => c != '\x3A');
                if (stringReader.PeekChar() != '\x3A')
                {
                    return false;
                }
                stringReader.Read();

                stringReader.SkipWhiteSpace();

                var value = stringReader.ReadToEnd();

                onHeaderLine(header, value ?? string.Empty);

                return true;
            }
        }
    }
}
