namespace Flux.Saea
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Sockets;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading;

    public class Buffer : IDisposable
    {
        private const int DefaultBufferSize = 1024*1024;
        private const int DefaultBlockSize = 1024;
        private readonly int _bufferSize;
        private readonly int _blockSize;
        private readonly byte[] _buffer;
        private readonly GCHandle _bufferPin;
        private int _pointer;
        private bool _exhausted;


        public Buffer(int bufferSize, int blockSize)
        {
            _bufferSize = bufferSize;
            _buffer = new byte[bufferSize];
            _bufferPin = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
            _blockSize = blockSize;
        }

        public void Alloc(SocketAsyncEventArgs args)
        {
            if (_exhausted)
            {
                args.SetBuffer(new byte[_blockSize], 0, _blockSize);
                return;
            }
            var pointer = Interlocked.Add(ref _pointer, _blockSize);
            if (pointer >= _bufferSize)
            {
                _exhausted = true;
                args.SetBuffer(new byte[_blockSize], 0, _blockSize);
                return;
            }
            args.SetBuffer(_buffer, pointer, _blockSize);
        }

        public void Dispose()
        {
            _bufferPin.Free();
        }
    }
}