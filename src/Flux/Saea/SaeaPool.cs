using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Flux.Saea
{
    public class SaeaPool
    {
        private readonly int _size;
        private readonly Stack<SocketAsyncEventArgs> _stack;
        private readonly object _syncRoot;

        private SaeaPool(int size)
        {
            _size = size;
            _stack = new Stack<SocketAsyncEventArgs>(size);
            _syncRoot = ((ICollection) _stack).SyncRoot;
        }

        public int Size
        {
            get { return _size; }
        }

        public static SaeaPool Create(int size)
        {
            var pool = new SaeaPool(size);
            for (int i = 0; i < size; i++)
            {
                var saea = new SocketAsyncEventArgs();
                pool._stack.Push(saea);
            }
            return pool;
        }

        public SocketAsyncEventArgs Get()
        {
            lock (_syncRoot)
            {
                return _stack.Pop();
            }
        }

        public void Release(SocketAsyncEventArgs saea)
        {
            lock (_syncRoot)
            {
                _stack.Push(saea);
            }
        }
    }
}