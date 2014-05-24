namespace Flux
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;

    public class DataPool : IDisposable
    {
        private const int DefaultBufferSize = 1024*1024;
        private const int DefaultBlockSize = 1024;
        private readonly int _bufferSize;
        private readonly int _blockSize;
        private readonly byte[] _buffer;
        private readonly GCHandle _bufferPin;
        private int _pointer;

        /// <summary>
        /// Initializes a new instance of the <see cref="Buffer"/> class.
        /// </summary>
        public DataPool() : this(DefaultBufferSize, DefaultBlockSize)
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Buffer"/> class.
        /// </summary>
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <param name="blockSize">Size of blocks to allocate.</param>
        public DataPool(int bufferSize, int blockSize)
        {
            _bufferSize = bufferSize;
            _buffer = new byte[bufferSize];
            _bufferPin = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
            _blockSize = blockSize;
            _pointer = 0 - blockSize;
        }

        /// <summary>
        /// Gets a pointer to the next block of available buffer
        /// </summary>
        /// <returns></returns>
        public int GetNextPointer()
        {
            int next;
            do
            {
                next = Interlocked.Add(ref _pointer, BlockSize) % _bufferSize;
                if (next < 0) next += _bufferSize;
            } while (_buffer[next] != 0);
            _buffer[next] = 1;
            return next;
        }

        /// <summary>
        /// Gets the buffer.
        /// </summary>
        /// <value>
        /// The buffer.
        /// </value>
        public byte[] Buffer
        {
            get { return _buffer; }
        }

        public int BlockSize
        {
            get { return _blockSize; }
        }

        public void Dispose()
        {
// ReSharper disable once ImpureMethodCallOnReadonlyValueField
            _bufferPin.Free();
        }
    }
}
