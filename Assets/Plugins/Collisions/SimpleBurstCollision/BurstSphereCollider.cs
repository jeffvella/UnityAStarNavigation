using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;


namespace Vella.SimpleBurstCollision
{
    [DebuggerDisplay("Center = {Center}, Radius={Radius}")]
    public struct BurstSphereCollider : IBurstCollider
    {
        public Vector3 Center;
        public Vector3 Offset;
        public float Radius;
        public float Scale;

        public static readonly BurstSphereCollider Empty = new BurstSphereCollider();

        public Bounds Bounds;
    }

    public static class BurstSphereColliderExtensions
    {
        public static List<BurstSphereCollider> Intersects(this BurstSphereCollider testCollider, NativeArray<BurstSphereCollider> colliders)
        {
            return IntersectionJobs.SphereSphereIntersectionJob.Execute(testCollider, colliders);
        }

        public static bool Contains(this BurstSphereCollider sphere, Vector3 worldPosition)
        {
            return math.distance(worldPosition, sphere.Center + sphere.Offset) <= sphere.Radius * sphere.Scale;
        }

        public static Vector3 ClosestPosition(this BurstSphereCollider sphere, Vector3 worldPosition)
        {
            return (worldPosition - sphere.Center + sphere.Offset) * sphere.Radius * sphere.Scale;
        }

        public static BurstBaseCollider ToBaseCollider(this BurstSphereCollider sphere) => new BurstBaseCollider
        {
            Type = BurstColliderType.Sphere,
            Sphere = sphere
        };

        public static BurstBaseCollider ToBaseCollider(this BurstSphereCollider sphere, int id) => new BurstBaseCollider
        {
            Type = BurstColliderType.Sphere,
            Sphere = sphere,
            Id = id,
        };
    }



}
