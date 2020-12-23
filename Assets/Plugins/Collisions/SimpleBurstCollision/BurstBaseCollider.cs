using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Vella.SimpleBurstCollision
{
    public enum BurstColliderType
    {
        None = 0,
        Sphere,
        Box,
    }

    public struct BurstBaseCollider : IBurstCollider
    {
        public BurstColliderType Type;
        public BurstSphereCollider Sphere;
        public BurstBoxCollider Box;
        public int Id;

        public BurstBaseCollider(Collider collider)
        {
            this = BurstColliderFactory.CreateFromCollider(collider);
        }

        public void Update(Collider collider)
        {
            if (collider is SphereCollider sphere)
                Sphere = BurstColliderFactory.CreateSphere(sphere);

            if (collider is BoxCollider box)
                Box = BurstColliderFactory.CreateBox(box);
        }

        public static readonly BurstBaseCollider Empty = new BurstBaseCollider();

    }

    public static class BurstBaseColliderExtensions
    {

        public static NativeList<BurstBaseCollider> Intersects(this BurstBaseCollider testCollider, BurstBaseCollider[] colliders)
        {
            return IntersectionJobs.BaseIntersectionJob.Execute(testCollider, colliders);
        }

        public static NativeList<BurstBaseCollider> Intersects(this BurstBaseCollider testCollider, NativeArray<BurstBaseCollider> colliders)
        {
            return IntersectionJobs.BaseIntersectionJob.Execute(testCollider, colliders);
        }

        public static bool Intersects(this BurstBaseCollider testCollider, BurstBaseCollider other)
        {
            switch (testCollider.Type)
            {
                case BurstColliderType.Box:
                    switch (other.Type)
                    {
                        case BurstColliderType.Box:
                            return IntersectionUtility.BoxBoxIntersects(testCollider.Box, other.Box);
                        case BurstColliderType.Sphere:
                            return IntersectionUtility.BoxSphereIntersects(testCollider.Box, other.Sphere);
                        default:
                            throw new InvalidOperationException();
                    }
                case BurstColliderType.Sphere:
                    switch (other.Type)
                    {
                        case BurstColliderType.Box:
                            return IntersectionUtility.BoxSphereIntersects(other.Box, testCollider.Sphere);
                        case BurstColliderType.Sphere:
                            return IntersectionUtility.SphereSphereIntersects(testCollider.Sphere, other.Sphere);
                        default:
                            throw new InvalidOperationException();
                    }
            }
            throw new InvalidOperationException();
        }

        public static Vector3 ClosestPosition(this BurstBaseCollider baseCollider, Vector3 worldPosition)
        {
            switch (baseCollider.Type)
            {
                case BurstColliderType.Box:
                    return baseCollider.Box.ClosestPosition(worldPosition);
                case BurstColliderType.Sphere:
                    return baseCollider.Sphere.ClosestPosition(worldPosition);
                default:
                    throw new InvalidOperationException();
            }
        }

        public static bool Contains(this BurstBaseCollider baseCollider, Vector3 worldPosition)
        {
            switch (baseCollider.Type)
            {
                case BurstColliderType.Box:
                    return baseCollider.Box.Contains(worldPosition);
                case BurstColliderType.Sphere:
                    return baseCollider.Sphere.Contains(worldPosition);
                default:
                    throw new InvalidOperationException();
            }
        }

        public static BurstBaseCollider ToBurstCollider(this Collider collider)
        {
            return new BurstBaseCollider(collider);
        }
    }
}

