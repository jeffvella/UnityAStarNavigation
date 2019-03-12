using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Providers.Grid;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.UIElements;

namespace Unity.Collections
{
    public struct NativeArray3D<T> : IDisposable, IEnumerable<T> where T : unmanaged
    {
        public NativeArray<T> Internal;
        internal readonly int _yLength;
        internal readonly int _xLength;
        internal readonly int _zLength;
        internal readonly int _yzLength;

        public NativeArray3D(int x, int y, int z, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            Internal = new NativeArray<T>(x * y * z, allocator);
            IsDisposed = false;
            _xLength = x;
            _yLength = y;
            _zLength = z;
            _yzLength = y * z;            
        }

        public T this[int i] => Internal[i];

        public T this[int x, int y, int z]
        {
            get => Internal[GetIndex(x, y, z)];
            set => Internal[GetIndex(x, y, z)] = value;
        }

        public int GetIndex(int x, int y, int z)
        {
            /*
             
            (2D) 4x2 Layout:

                y0   y1
            x0: #0 | #1     
            x1: #2 | #3
            x2: #4 | #5
            x3: #6 | #7

            >> x * yLen + y

            [0,0] = 0 * 2 + 0 = 0
            [0,1] = 0 * 2 + 1 = 1
            [1,0] = 1 * 2 + 0 = 2
            [1,1] = 1 * 2 + 1 = 3
            [2,0] = 2 * 2 + 0 = 4
            [2,1] = 2 * 2 + 1 = 5
            [3,0] = 3 * 2 + 0 = 6
            [3,1] = 3 * 2 + 1 = 7

            (3D) 2x2x2 Layout:

                 y1       y2
                 z0   z1  z0   z1 
            x0: #0 / #1 | #3 / #4 
            x1: #5 / #6 | #7 / #8

            >> (x * yLen * zLen) + (y * zLen) + z
            
            [0,0,0] = (0 * 2 * 2) + (0 * 2) + 0 = 0
            [0,0,1] = (0 * 2 * 2) + (0 * 2) + 1 = 1
 
            [0,1,0] = (0 * 2 * 2) + (1 * 2) + 0 = 2
            [0,1,1] = (0 * 2 * 2) + (1 * 2) + 1 = 3

            [1,0,0] = (1 * 2 * 2) + (0 * 2) + 0 = 4
            [1,0,1] = (1 * 2 * 2) + (0 * 2) + 1 = 5

            [1,1,0] = (1 * 2 * 2) + (1 * 2) + 0 = 6
            [1,1,1] = (1 * 2 * 2) + (1 * 2) + 1 = 7

            (3D) 2x2x3 Layout:

            >> (x * yLen * zLen) + (y * zLen) + z
            
                y0                 y1
                z0    z1    z2     z0   z1    z2
            x0: #0  / #1  / #2  | #3  / #4  / #5
            x1: #6  / #7  / #8  | #9  / #10 / #11

            [0,0,0] = (0 * 2 * 3) + (0 * 3) + 0 = 0
            [0,0,1] = (0 * 2 * 3) + (0 * 3) + 1 = 1
            [0,0,2] = (0 * 2 * 3) + (0 * 3) + 2 = 2
                                                                  
            [0,1,0] = (0 * 2 * 3) + (1 * 3) + 0 = 3
            [0,1,1] = (0 * 2 * 3) + (1 * 3) + 1 = 4
            [0,1,2] = (0 * 2 * 3) + (1 * 3) + 2 = 5

            [1,0,0] = (1 * 2 * 3) + (0 * 3) + 0 = 6
            [1,0,1] = (1 * 2 * 3) + (0 * 3) + 1 = 7
            [1,0,2] = (1 * 2 * 3) + (0 * 3) + 2 = 8

            [1,1,0] = (1 * 2 * 3) + (1 * 3) + 0 = 9
            [1,1,1] = (1 * 2 * 3) + (1 * 3) + 1 = 10
            [1,1,2] = (1 * 2 * 3) + (1 * 3) + 2 = 11

            (3D) 3x3x3 Layout:
            
                y0                y1                y2
                z0    z1    z2    z0   z1    z2     z0   z1    z2    
            x0: #0  / #1  / #3  | #4  / #5  / #6  | #7  / #8  / #9 
            x1: #10 / #11 / #12 | #13 / #14 / #15 | #16 / #17 / #18 
            x2: #19 / #20 / #21 | #22 / #23 / #24 | #25 / #26 / #27 

            */

            return x * _yzLength + y * _zLength + z;

        }

        public int3 Get3DIndexes(int idx)
        {
            int x = idx / (_yzLength);
            idx -= (x * _yzLength);
            int y = idx / _zLength;
            int z = idx % _zLength;
            return new int3(x, y, z);
        }

        //public unsafe ref T ByRefUnsafe(void* ptr, int i)
        //{
        //    return ref ((T*)ptr)[i];
        //}

        //public unsafe ref T ByRefUnsafe(void* ptr, int x, int y, int z)
        //{   
        //    return ref ((T*)ptr)[x * _yzLength + y * _zLength + z];
        //}

        public unsafe ref T AsRef(int x, int y, int z)
        {
            var ptr = Internal.GetUnsafePtr();
            var idx = GetIndex(x, y, z);
            return ref ((T*)ptr)[idx];
        }

        public int GetLength(int index)
        {
            switch (index)
            {
                case 0: return _xLength;
                case 1: return _yLength;
                case 2: return _zLength;
            }
            throw new ArgumentOutOfRangeException($"The dimension with Length index '{index}' doesn't exist");
        }

        public int Length => Internal.Length;
        public bool IsCreated => Internal.IsCreated;


        public void Dispose()
        {
            IsDisposed = true;
            Internal.Dispose();
        }

        public bool IsDisposed { get; private set; }

        public IEnumerator<T> GetEnumerator() => Internal.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    }

}
