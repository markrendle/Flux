namespace Flux
{
    using System.Threading;

    public class BufferBot
    {
        private readonly int _bufferSize;
        private readonly int _blockSize;
        private readonly byte[] _buffer;
        private int _pointer;

        /// <summary>
        /// Initializes a new instance of the <see cref="Buffer"/> class.
        /// </summary>
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <param name="blockSize">Size of blocks to allocate.</param>
        public BufferBot(int bufferSize, int blockSize)
        {
            _bufferSize = bufferSize;
            _buffer = new byte[bufferSize];
            _blockSize = blockSize;
            _pointer = 0 - blockSize;
        }

        /// <summary>
        /// Gets a pointer to the next block of available buffer
        /// </summary>
        /// <returns></returns>
        public int GetNextPointer()
        {
            int next = Interlocked.Add(ref _pointer, _blockSize) % _bufferSize;
            return next < 0 ? next + _bufferSize : next;
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
    }
}
