using System;
using System.Collections.Generic;
using System.Linq;
using SimpleBurstCollision;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Vella.SimpleBurstCollision
{
    public static class IntersectionJobs
    {
        [BurstCompile]
        public struct BaseIntersectionJob : IJob
        {
            public BurstBaseCollider TestCollider;
            public NativeArray<BurstBaseCollider> OtherColliders;
            public NativeList<BurstBaseCollider> ResultCollisions;

            public void Execute()
            {
                for (int i = 0; i < OtherColliders.Length; i++)
                {
                    ref var other = ref OtherColliders.AsRef(i);
                    if (TestCollider.Intersects(other))
                    {
                        ResultCollisions.Add(other);
                    }
                }
            }

            public static NativeList<BurstBaseCollider> Execute(BurstBaseCollider testCollider, BurstBaseCollider[] otherColliders)
            {
                using (var nativeArray = new NativeArray<BurstBaseCollider>(otherColliders, Allocator.TempJob))
                {
                    return Execute(testCollider, nativeArray);
                }
            }

            public static NativeList<BurstBaseCollider> Execute(BurstBaseCollider testCollider, NativeArray<BurstBaseCollider> otherColliders)
            {
                var resultStartingSize = (int)Math.Abs(Math.Floor(otherColliders.Length * 0.1f));
                var result = new NativeList<BurstBaseCollider>(resultStartingSize, Allocator.TempJob);
                new BaseIntersectionJob
                {
                    TestCollider = testCollider,
                    OtherColliders = otherColliders,
                    ResultCollisions = result,

                }.Schedule().Complete();
                return result;
            }
        }

        [BurstCompile]
        public struct SphereSphereIntersectionJob : IJob
        {
            [ReadOnly]
            public BurstSphereCollider TestCollider;

            public NativeArray<BurstSphereCollider> OtherColliders;
       
            public NativeList<BurstSphereCollider> NativeResultCollisions;            

            public void Execute()
            {
                for (int i = 0; i < OtherColliders.Length; i++)
                {
                    ref var other = ref OtherColliders.AsRef(i);
                    if (DistanceSqr(ref TestCollider, other))
                    { 
                        NativeResultCollisions.Add(other);
                    }
                }    
            }

            public static bool DistanceSqr(ref BurstSphereCollider a, BurstSphereCollider b)
            {
                var combinedRadius = a.Radius + b.Radius;
                var offset = a.Center - b.Center;
                var sqrLen = offset.x * offset.x + offset.y * offset.y + offset.z * offset.z;
                return sqrLen <= combinedRadius * combinedRadius;
            }

            public static List<BurstSphereCollider> Execute(BurstSphereCollider testCollider, NativeArray<BurstSphereCollider> otherColliders)
            { 
                using (var result = new NativeList<BurstSphereCollider>(otherColliders.Length, Allocator.TempJob))
                {
                    new SphereSphereIntersectionJob
                    {
                        TestCollider = testCollider,
                        OtherColliders = otherColliders,
                        NativeResultCollisions = result,

                    }.Schedule().Complete();
                    return result.ToArray().ToList();
                }                
            }
        }

    }
}
