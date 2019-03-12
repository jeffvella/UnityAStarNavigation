//using System;
//using Providers.Grid;
//using Unity.Burst;
//using Unity.Collections;
//using Unity.Collections.LowLevel.Unsafe;
//using Unity.Jobs;
//using Unity.Mathematics;
//using UnityEngine;


//public static class GridNodeJobs
//{
//    public static class AllNodes
//    {
//        [BurstCompile]
//        public struct SetFlags : IJob
//        {
//            [NativeDisableUnsafePtrRestriction]
//            public IntPtr BaseAddress;
//            public ulong Flags;
//            public int Stride;
//            public int Length;

//            public unsafe void Execute()
//            {
//                for (int i = 0; i < Length; i++)
//                {
//                    ((GridNode*)(BaseAddress + i * Stride))->Flags = Flags;
//                }
//            }

//            public static unsafe JobHandle Schedule(NativeArray<GridNode> array, NodeFlags defaultFlags)
//            {
//                return new SetFlags
//                {
//                    Flags = (ulong)defaultFlags,
//                    BaseAddress = (IntPtr)array.GetUnsafePtr(),
//                    Stride = UnsafeUtility.SizeOf<GridNode>(),
//                    Length = array.Length

//                }.Schedule();
//            }
//        }
//    }
//}

//public static class GridNodeJobExtensions
//{
//    public static unsafe float DistanceJob(this GridNode a, GridNode b)
//    {
//        var resultContainer = new FloatResultContainer();
//        var job = new GetDistanceJob
//        {
//            NodeA = &a,
//            NodeB = &b,
//            Result = &resultContainer,
//        };
//        var handle = job.Schedule();
//        handle.Complete();
//        return resultContainer.Result;
//    }

//    public struct FloatResultContainer
//    {
//        public float Result;
//    }

//    [BurstCompile]
//    public unsafe struct GetDistanceJob : IJob
//    {
//        [NativeDisableUnsafePtrRestriction]
//        public GridNode* NodeA;
//        [NativeDisableUnsafePtrRestriction]
//        public GridNode* NodeB;
//        [NativeDisableUnsafePtrRestriction]
//        public FloatResultContainer* Result;

//        public void Execute()
//        {
//            var currentX = Mathf.Abs(NodeA->NavigableCenter.x - NodeB->NavigableCenter.x);
//            var currentY = Mathf.Abs(NodeA->NavigableCenter.y - NodeB->NavigableCenter.y);
//            var currentZ = Mathf.Abs(NodeA->NavigableCenter.z - NodeB->NavigableCenter.z);
//            Result->Result = Mathf.Sqrt(currentX + currentY + currentZ);
//        }
//    }

//    [BurstCompile]
//    public struct GetDistanceJob2 : IJob
//    {
//        public float Ax;
//        public float Ay;
//        public float Az;
//        public float Bx;
//        public float By;
//        public float Bz;    

//        public float Result;
//        private float _xD;
//        private float _yD;
//        private float _zD;

//        public void Execute()
//        {
//            _xD = Ax - Bx;
//            _yD = Ay - By;
//            _zD = Az - Bz;

//            Result = _xD < 0 ? -_xD : _xD +
//                     _yD < 0 ? -_yD : _yD + 
//                     _zD < 0 ? -_zD : _zD;
//        }

//        public static float Complete(float aX, float aY, float aZ, float bX, float bY, float bZ)
//        {
//            var job = new GetDistanceJob2
//            {
//                Ax = aX,
//                Ay = aY,
//                Az = aZ,
//                Bx = bX,
//                By = bY,
//                Bz = bZ
//            };
//            var handle = job.Schedule();
//            handle.Complete();
//            return job.Result;
//        }
//    }
//}


