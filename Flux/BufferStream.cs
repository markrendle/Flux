namespace Flux
{
    using System;
    using System.IO;

    internal sealed class BufferStream : Stream
    {
        private readonly Lazy<Stream> _lazyStream = new Lazy<Stream>(() => new MemoryStream()); 

        internal Stream InternalStream
        {
            get { return _lazyStream.Value; }
        }

        public BufferStream()
        {
            _lazyStream = new Lazy<Stream>(() => new MemoryStream());
        }

        public BufferStream(Stream stream)
        {
            _lazyStream = new Lazy<Stream>(() => stream);
        }

        public override void Flush()
        {
            InternalStream.Flush();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            InternalStream.Write(buffer, offset, count);
        }

        public override long Length
        {
            get
            {
                return _lazyStream.IsValueCreated ? _lazyStream.Value.Length : 0;
            }
        }

        public override long Position
        {
            get
            {
                return _lazyStream.IsValueCreated ? _lazyStream.Value.Position : 0;
            }
            set { InternalStream.Position = value; }
        }

        public override void SetLength(long value)
        {
            InternalStream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return InternalStream.Read(buffer, offset, count);
        }

        public override bool CanRead
        {
            get { return InternalStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return InternalStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return InternalStream.CanWrite; }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return InternalStream.Seek(offset, origin);
        }

        public override void Close()
        {
        }
        protected override void Dispose(bool disposing)
        {
        }
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return InternalStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return InternalStream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return InternalStream.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            InternalStream.EndWrite(asyncResult);
        }

        public override int ReadByte()
        {
            return InternalStream.ReadByte();
        }

        public override int ReadTimeout
        {
            get
            {
                return InternalStream.ReadTimeout;
            }
            set
            {
                InternalStream.ReadTimeout = value;
            }
        }

        internal void ForceDispose()
        {
            if (_lazyStream.IsValueCreated)
            {
                _lazyStream.Value.Dispose();
            }
        }

        internal bool TryGetBuffer(out byte[] buffer)
        {
            if (_lazyStream.IsValueCreated)
            {
                var memoryStream = InternalStream as MemoryStream;
                if (memoryStream != null)
                {
                    buffer = memoryStream.GetBuffer();
                    return true;
                }
            }
            buffer = null;
            return false;
        }
    }
}