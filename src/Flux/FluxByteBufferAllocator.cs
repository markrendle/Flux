using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using LibuvSharp;

namespace Flux
{
    public class FluxByteBufferAllocator : ByteBufferAllocatorBase
    {
        private BufferPin _pin;
        public override int Alloc(int size, out IntPtr ptr)
        {
            if (_pin == null)
            {
                _pin = new BufferPin(size);
            }
            else if (_pin.Buffer.Length < size)
            {
                _pin.Dispose();
                _pin = new BufferPin(size);
            }
            ptr = _pin.Start;
            return _pin.Count.ToInt32();
        }

        public override void Dispose(bool disposing)
        {
            if (_pin != null)
            {
                _pin.Dispose();
            }
            _pin = null;
        }

        public override ArraySegment<byte> Retrieve(int size)
        {
            var segment = BytePool.Intance.Get(size);
            Array.Copy(_pin.Buffer, 0, segment.Array, segment.Offset, size);
            return segment;
        }
    }

    public class BytePool : IDisposable
    {
        private static readonly BytePool _instance = new BytePool();
        private BufferPin _pin;
        private readonly ConcurrentStack<int> _extraSmallPointers;
        private readonly ConcurrentStack<int> _smallPointers;
        private readonly ConcurrentStack<int> _mediumPointers;
        private readonly ConcurrentStack<int> _largePointers;

        private BytePool()
        {
            var extraSmallPointers = Enumerable.Range(0, 1024).Select(e => e * 256).ToArray();
            int offset = extraSmallPointers.Last() + 256;
            var smallPointers = Enumerable.Range(0, 1024).Select(e => (e * 512) + offset).ToArray();
            offset = smallPointers.Last() + 512;
            var mediumPointers = Enumerable.Range(0, 1024).Select(e => (e * 1024) + offset).ToArray();
            offset = mediumPointers.Last() + 1024;
            var largePointers = Enumerable.Range(0, 1024).Select(e => (e * 2048) + offset).ToArray();
            int size = largePointers.Last() + 2048;
            _pin = new BufferPin(size);

            _extraSmallPointers = new ConcurrentStack<int>(extraSmallPointers);
            _smallPointers = new ConcurrentStack<int>(smallPointers);
            _mediumPointers = new ConcurrentStack<int>(mediumPointers);
            _largePointers = new ConcurrentStack<int>(largePointers);
        }

        public static BytePool Intance
        {
            get { return _instance; }
        }

        public ArraySegment<byte> Get(int size)
        {
            ArraySegment<byte> segment;
            if (size < 257) segment = Get(_extraSmallPointers, size);
            else if (size < 513) segment = Get(_smallPointers, size);
            else if (size < 1025) segment = Get(_mediumPointers, size);
            else if (size < 2049) segment = Get(_largePointers, size);
            else segment = CreateEmergencySegment(size);
            return segment;
        }

        public void Free(ArraySegment<byte> segment)
        {
            // Offset of one means an emergency array, no reuse.
            if (segment.Offset == 1) return;
            if (segment.Count < 257) _extraSmallPointers.Push(segment.Offset);
            else if (segment.Count < 513) _smallPointers.Push(segment.Offset);
            else if (segment.Count < 1025) _mediumPointers.Push(segment.Offset);
            else if (segment.Count < 2049) _largePointers.Push(segment.Offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ArraySegment<byte> Get(ConcurrentStack<int> stack, int size)
        {
            int offset;
            if (stack.TryPop(out offset))
            {
                return new ArraySegment<byte>(_pin.Buffer, offset, size);
            }
            return CreateEmergencySegment(size);
        }

        private static ArraySegment<byte> CreateEmergencySegment(int size)
        {
            return new ArraySegment<byte>(new byte[size + 1], 1, size);
        }

        public void Dispose()
        {
            if (_pin == null) return;
            _pin.Dispose();
            _pin = null;
        }
    }
}