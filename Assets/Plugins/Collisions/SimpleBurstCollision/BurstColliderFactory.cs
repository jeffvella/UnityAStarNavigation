using System;
using Navigation.Scripts.Region;
using Unity.Mathematics;
using UnityEngine;

namespace Vella.SimpleBurstCollision
{   
    public static class BurstColliderFactory
    {
        public static BurstBaseCollider CreateFromCollider(Collider inputCollider)
        {
            var result = new BurstBaseCollider();
            result.Id = inputCollider.gameObject.GetInstanceID();
            switch (inputCollider)
            {
                case SphereCollider sphere:
                    result.Type = BurstColliderType.Sphere;
                    result.Sphere = CreateSphere(sphere);
                    break;
                case BoxCollider box:
                    result.Type = BurstColliderType.Box;
                    result.Box = CreateBox(box);
                    break;
                default:
                    throw new InvalidOperationException();
            }
            return result;
        }

        public static BurstSphereCollider CreateSphere(SphereCollider sphereCollider) => new BurstSphereCollider
        {
            Scale = GetLargestAxis(sphereCollider.transform.localScale),
            Center = sphereCollider.transform.position,
            Radius = sphereCollider.radius,
            Offset = sphereCollider.center,
        };

        private static float GetLargestAxis(Vector3 v)
        {           
            if (v.x > v.y && v.x > v.z) return v.x;
            if (v.y > v.x && v.y > v.z) return v.y;
            return v.z;
        }

        public static BurstSphereCollider CreateSphere(Vector3 center, float radius) => new BurstSphereCollider
        {
            Center = center,
            Radius = radius,
            Scale =  1,
        };

        public static BurstSphereCollider CreateSphere(Vector3 center, Vector3 offset, float radius, float scale) => new BurstSphereCollider
        {
            Center = center,
            Offset = offset,
            Radius = radius,
            Scale = scale
        };

        public static BurstBoxCollider CreateBox(BoxCollider c)
        {
            return CreateBox(c.transform);
        }

        public static BurstBoxCollider CreateBox(Transform t)
        {
            var matrix = new float4x4(t.rotation, t.position);
            return BurstBoxCollider(t.position, t.lossyScale, t.rotation, matrix);       
        }

        public static BurstBoxCollider CreateBox(Vector3 center, Vector3 size, Quaternion rotation)
        {
            var matrix = math.mul(new float4x4(rotation, center), float4x4.Scale(size));
            return BurstBoxCollider(center, size, rotation, matrix);
        }

        private static BurstBoxCollider BurstBoxCollider(Vector3 center, Vector3 size, Quaternion rotation, float4x4 matrix)
        {
            var max = size * 0.5f;
            var min = -max;

            var box =  new BurstBoxCollider
            {
                Bounds = new Bounds(center, size),
                Center = center,
                Right = rotation * Vector3.right,
                Up = rotation * Vector3.up,
                Forward = rotation * Vector3.forward,
                Max = max,
                Min = min,
                ToWorldMatrix = matrix,
                ToLocalMatrix = math.inverse(matrix),

                V1 = center + rotation * min,
                V2 = center + rotation * new Vector3(max.x, min.y, min.z),
                V3 = center + rotation * new Vector3(min.x, max.y, min.z),
                V4 = center + rotation * new Vector3(max.x, max.y, min.z),
                V5 = center + rotation * new Vector3(min.x, min.y, max.z),
                V6 = center + rotation * new Vector3(max.x, min.y, max.z),
                V7 = center + rotation * new Vector3(min.x, max.y, max.z),
                V8 = center + rotation * max,
            };

            box.UpdateBounds();
            return box;
        }
    }

}

