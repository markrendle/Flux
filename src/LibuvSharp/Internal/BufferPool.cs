using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LibuvSharp
{
    public class BufferPool : IDisposable
    {
        private readonly int _segmentSize;
        private readonly int _segmentCount;
        private readonly byte[] _bytes;
        private readonly GCHandle _handle;
        private readonly Stack<int>

        public BufferPool(int segmentSize, int segmentCount)
        {
            _segmentSize = segmentSize;
            _segmentCount = segmentCount;
            _bytes = new byte[segmentSize * segmentCount];
            _handle = GCHandle.Alloc(_bytes, GCHandleType.Pinned);
        }

        public void Dispose()
        {
            if (_handle.IsAllocated)
            {
                _handle.Free();
            }
        }
    }
}