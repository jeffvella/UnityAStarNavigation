using System;
using System.Diagnostics;
using Navigation.Scripts.Region;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace Vella.SimpleBurstCollision
{
    public struct BurstBoxCollider : IBurstCollider
    {
        public float4x4 ToLocalMatrix;
        public float4x4 ToWorldMatrix;

        public Bounds Bounds;

        public Vector3 Right;
        public Vector3 Up;
        public Vector3 Forward;
        public Vector3 Center;
        public Vector3 Max;
        public Vector3 Min;

        public Vector3 V1;
        public Vector3 V2;
        public Vector3 V3;
        public Vector3 V4;
        public Vector3 V5;
        public Vector3 V6;
        public Vector3 V7;
        public Vector3 V8;

        public static readonly BurstBoxCollider Empty = new BurstBoxCollider();
    }

    public static class BurstBoxColliderExtensions
    {
        public static void UpdateBounds(this BurstBoxCollider box)
        {
            box.Bounds = new Bounds();
            box.Bounds.Encapsulate(box.V1);
            box.Bounds.Encapsulate(box.V2);
            box.Bounds.Encapsulate(box.V3);
            box.Bounds.Encapsulate(box.V4);
            box.Bounds.Encapsulate(box.V5);
            box.Bounds.Encapsulate(box.V6);
            box.Bounds.Encapsulate(box.V7);
            box.Bounds.Encapsulate(box.V8);
        }

        public static Vector3 ClosestPosition(this BurstBoxCollider box, Vector3 worldPosition)
        {
            return ClosestPosition(box, worldPosition, box.ToLocalMatrix, box.ToWorldMatrix);
        }

        public static Vector3 ClosestPosition(this BurstBoxCollider box, Vector3 worldPosition, float4x4 toLocal, float4x4 toWorld)
        {
            var localSphereCenter = math.transform(toLocal, worldPosition);
            var closest = box.ClosestLocalPosition(localSphereCenter);
            return math.transform(toWorld, closest);
        }

        public static Vector3 ClosestPosition(this BurstBoxCollider box, Vector3 worldPosition, float4x4 toLocal)
        {
            var localPosition = math.transform(toLocal, worldPosition);
            var closest = box.ClosestLocalPosition(localPosition);
            return math.transform(math.inverse(box.ToWorldMatrix), closest);
        }

        private static Vector3 ClosestLocalPosition(this BurstBoxCollider box, float3 localPosition)
        {
            var closestX = Mathf.Max(box.Min.x, Mathf.Min(localPosition.x, box.Max.x));
            var closestY = Mathf.Max(box.Min.y, Mathf.Min(localPosition.y, box.Max.y));
            var closestZ = Mathf.Max(box.Min.z, Mathf.Min(localPosition.z, box.Max.z));
            var closest = new Vector3(closestX, closestY, closestZ);
            return closest;
        }

        public static bool Contains(this BurstBoxCollider box, Vector3 worldPosition)
        {
            var localPosition = box.GetLocalPosition(worldPosition);
            return localPosition.x > box.Min.x && localPosition.x < box.Max.x &&
                   localPosition.y > box.Min.y && localPosition.y < box.Max.y &&
                   localPosition.z > box.Min.z && localPosition.z < box.Max.z;
        }

        public static bool Contains(this BurstBoxCollider box, Vector3 worldPosition, float4x4 matrix)
        {
            var localPosition = math.transform(matrix, worldPosition);
            return localPosition.x > box.Min.x && localPosition.x < box.Max.x &&
                   localPosition.y > box.Min.y && localPosition.y < box.Max.y &&
                   localPosition.z > box.Min.z && localPosition.z < box.Max.z;
        }

        public static Vector3 GetWorldPosition(this BurstBoxCollider box, Vector3 localPosition)
        {
            return math.transform(box.ToWorldMatrix, localPosition);
        }

        public static Vector3 GetLocalPosition(this BurstBoxCollider box, Vector3 worldPosition)
        {
            return math.transform(box.ToLocalMatrix, worldPosition);
        }

        public static BurstBaseCollider ToBaseCollider(this BurstBoxCollider box) => new BurstBaseCollider
        {
            Type = BurstColliderType.Box,
            Box = box
        };
    }
}