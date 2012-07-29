namespace Flux
{
    using System;
    using System.IO;

    internal sealed class BufferStream : Stream
    {
        private readonly Stream _stream;

        public BufferStream()
        {
            _stream = new MemoryStream();
        }

        public BufferStream(Stream stream)
        {
            _stream = stream;
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }

        public override long Length
        {
            get { return _stream.Length; }
        }

        public override long Position
        {
            get { return _stream.Position; }
            set { _stream.Position = value; }
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        public override bool CanRead
        {
            get { return _stream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return _stream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return _stream.CanWrite; }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void Close()
        {
        }
        protected override void Dispose(bool disposing)
        {
        }
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _stream.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _stream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return _stream.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            _stream.EndWrite(asyncResult);
        }

        public override int ReadByte()
        {
            return _stream.ReadByte();
        }

        public override int ReadTimeout
        {
            get
            {
                return _stream.ReadTimeout;
            }
            set
            {
                _stream.ReadTimeout = value;
            }
        }

        internal void ForceDispose()
        {
            _stream.Dispose();
        }

        internal Stream InternalStream
        {
            get { return _stream; }
        }

        internal bool TryGetBuffer(out byte[] buffer)
        {
            var memoryStream = _stream as MemoryStream;
            if (memoryStream != null)
            {
                buffer = memoryStream.GetBuffer();
                return true;
            }
            buffer = null;
            return false;
        }
    }
}