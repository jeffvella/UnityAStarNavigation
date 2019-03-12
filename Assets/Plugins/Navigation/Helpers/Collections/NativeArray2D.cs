using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections;

namespace Unity.Collections
{
    public struct NativeArray2D<T> : IDisposable, IEnumerable<T> where T : unmanaged
    {
        public NativeArray<T> Internal;
        private readonly int _yLength;
        private readonly int _xLength;

        public NativeArray2D(int x, int y, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            Internal = new NativeArray<T>(x * y, allocator);
            _yLength = y;
            _xLength = x;
        }

        public T this[int i] => Internal[i];

        public T this[int x, int y]
        {
            get => Internal[CalculateIndex(x, y)];
            set => Internal[CalculateIndex(x, y)] = value;
        }

        public int CalculateIndex(int x, int y)
        {
            return (x * _yLength) + y;
        }

        public unsafe ref T ByRef(int x, int y)
        {
            var ptr = Internal.GetUnsafePtr();
            var idx = CalculateIndex(x,y);
            return ref ((T*)ptr)[idx];
        }

        public int GetLength(int index)
        {
            switch (index)
            {
                case 0: return _xLength;
                case 1: return _yLength;                  
            }
            throw new ArgumentOutOfRangeException($"The dimension with Length index '{index}' doesn't exist");
        }

        public int Length => Internal.Length;
        public void Dispose() => Internal.Dispose();
        public IEnumerator<T> GetEnumerator() => Internal.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

