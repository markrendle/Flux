#region License
//
// Http Helpers Library: StringReaderExtensions.cs
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
using System.Text;
#endregion

namespace HttpHelpers.Extensions
{
    static class StringReaderExtensions
    {
        public static string TakeWhile(this StringReader reader, Func<char, bool> predicate)
        {
            var builder = new StringBuilder();
            while (true)
            {
                var raw = reader.Peek();
                if (raw == -1)
                {
                    break;
                }
                var ch = (char)raw;
                if (!predicate(ch))
                {
                    break;
                }
                builder.Append(ch);
                reader.Read();
            }
            return builder.ToString();
        }

        public static void SkipWhiteSpace(this StringReader reader)
        {
            while (true)
            {
                var raw = reader.Peek();
                if (raw == -1)
                {
                    break;
                }
                var ch = (char)raw;
                if (!ch.IsWhiteSpace())
                {
                    break;
                }
                reader.Read();
            }
        }

        public static int SkipWhile(this StringReader reader, Func<char, bool> predicate)
        {
            var skipped = 0;
            while (true)
            {
                var raw = reader.Peek();
                if (raw == -1)
                {
                    break;
                }
                var ch = (char)raw;
                if (!predicate(ch))
                {
                    break;
                }
                skipped++;
                reader.Read();
            }
            return skipped;
        }

        public static char PeekChar(this StringReader reader)
        {
            return (char) reader.Peek();
        }
    }
}
