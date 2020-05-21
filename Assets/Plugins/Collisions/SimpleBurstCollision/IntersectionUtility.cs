using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Vella.SimpleBurstCollision
{
    public static class IntersectionUtility
    {
        public static bool BoxSphereIntersects(BurstBoxCollider box, BurstSphereCollider sphere)
        {
            var closestPosition = box.ClosestPosition(sphere.Center + sphere.Offset);
            var distance = Vector3.Distance(sphere.Center + sphere.Offset, closestPosition);
            return distance - sphere.Radius * sphere.Scale <= 0;
        }

        public static bool SphereSphereIntersects(BurstSphereCollider a, BurstSphereCollider b)
        {
            var combinedRadius = a.Radius * a.Scale + b.Radius * b.Scale;
            var distance = Vector3.Distance(a.Center + a.Offset, b.Center + b.Offset);
            return distance <= combinedRadius;
        }

        public static bool SphereSphereIntersects2(ref BurstSphereCollider a, BurstSphereCollider b)
        {
            var totalRadius = a.Radius + b.Radius;
            var len = a.Center - b.Center;
            var sqrLen = len.x * len.x + len.y * len.y + len.z * len.z;
            return sqrLen <= totalRadius * totalRadius;
        }

        public static bool BoxSphereIntersects(BurstBoxCollider box, BurstSphereCollider b, out IntersectionInfo info)
        {
            info = new IntersectionInfo();

            var closest = box.ClosestPosition(b.Center);
            var distToClosest = Vector3.Distance(b.Center, closest);
            var gapDistance = distToClosest - b.Radius;
            var centerToClosest = closest - b.Center;
            var overlapVector = (b.Radius - distToClosest) * centerToClosest.normalized;

            info.GapDistance = gapDistance;
            info.IsIntersecting = gapDistance <= 0 ? 1 : 0;
            info.PointOnB = closest + overlapVector;
            info.PointOnA = closest;

            if (gapDistance <= 0)
            {
                info.PenetrationDirection = overlapVector;
                info.PenetrationDistance = overlapVector.magnitude;
                return true;
            }

            info.GapDirection = overlapVector;
            return false;
        }

        //public struct CollisionTransform
        //{
        //    public Vector3 Center;
        //    public float4x4 Matrix;
        //    public DynamicBuffer<Vector3> CollisionAxes;
        //}

        //public static bool SATIntersects(CollisionTransform a, CollisionTransform b)
        //{
        //    for (int i = 0; i < a.CollisionAxes; i++)
        //    {

        //    }
        //}

    
        private static bool IsSeparated(Vector3[] aVerts, Vector3[] bVerts, Vector3 axis)
        {
            // Handles the cross product = {0,0,0} case
            if (axis == Vector3.zero)
                return false;

            var aMin = float.MaxValue;
            var aMax = float.MinValue;
            var bMin = float.MaxValue;
            var bMax = float.MinValue;

            // Check axis against each vertex combination >>

            for (int i = 0; i < aVerts.Length; i++)
            {
                for (int j = 0; j < bVerts.Length; j++)
                {
                    CalculateMinMax(axis, aVerts[i], bVerts[j], ref aMin, ref aMax, ref bMin, ref bMax);
                }
            }

            // One-dimensional intersection test between a and b
            var longSpan = Mathf.Max(aMax, bMax) - Mathf.Min(aMin, bMin);
            var sumSpan = aMax - aMin + bMax - bMin;
            return longSpan >= sumSpan; // > to treat touching as intersection
        }

        public class ConvexPolygon
        {
            public Face[] Faces;
            public Vector3 Vertices;
        }

        public class Face
        {
            public Vector3 Center;
            public Vector3 Normal;
        }

        public static bool ConvexMeshIntersects(Mesh objectA, float4x4 toWorldMatrixA, Mesh objectB, float4x4 toWorldMatrixB)
        {
            var aWorldSpaceVerts = objectA.vertices.Select(v => (Vector3)math.transform(toWorldMatrixA, v)).ToArray();
            var bWorldSpaceVerts = objectB.vertices.Select(v => (Vector3)math.transform(toWorldMatrixB, v)).ToArray();

            //Debug START;
            var aFaces = GetFaces(objectA, toWorldMatrixA);
            var bFaces = GetFaces(objectB, toWorldMatrixB);

            foreach (var face in aFaces)
            {
                Debug.DrawLine(face.Center, face.Center + face.Normal * 0.3f, Color.blue);
            }

            foreach (var face in bFaces)
            {
                Debug.DrawLine(face.Center, face.Center + face.Normal * 0.3f, Color.red);
            }
            // Debug STOP;

            for (int i = 0; i < objectA.triangles.Length; i = i + 3)
            {
                Vector3 a1 = aWorldSpaceVerts[objectA.triangles[i]];
                Vector3 a2 = aWorldSpaceVerts[objectA.triangles[i + 1]];
                Vector3 a3 = aWorldSpaceVerts[objectA.triangles[i + 2]];

                var faceNormal = Vector3.Cross(a3 - a2, a1 - a2).normalized;
                var faceNormalWorld = math.rotate(toWorldMatrixA, faceNormal.normalized);

                if (IsSeparated(aWorldSpaceVerts, bWorldSpaceVerts, faceNormalWorld))
                    return false;
            }

            for (int i = 0; i < objectB.triangles.Length; i = i + 3)
            {
                Vector3 a1 = bWorldSpaceVerts[objectB.triangles[i]];
                Vector3 a2 = bWorldSpaceVerts[objectB.triangles[i + 1]];
                Vector3 a3 = bWorldSpaceVerts[objectB.triangles[i + 2]];

                var faceNormal = Vector3.Cross(a3 - a2, a1 - a2).normalized;
                var faceNormalWorld = math.rotate(toWorldMatrixA, faceNormal.normalized);

                if (IsSeparated(aWorldSpaceVerts, bWorldSpaceVerts, faceNormalWorld))
                    return false;
            }

            return true;
        }

        private static List<Face> GetFaces(Mesh objectA, float4x4 toWorldMatrixA)
        {
            var result = new List<Face>();
            var verts = objectA.vertices;         
            var indices = objectA.triangles;            

            for (int i = 0; i < objectA.triangles.Length;)
            {
                var idx1 = i++;
                var idx2 = i++;
                var idx3 = i++;

                Vector3 p1 = verts[indices[idx1]];
                Vector3 p2 = verts[indices[idx2]];
                Vector3 p3 = verts[indices[idx3]];

                var center = ((p1 + p2 + p3) / 3);
                var norm = Vector3.Cross(p3 - p2, p1 - p2).normalized;                

                result.Add(new Face
                {
                    Center = math.transform(toWorldMatrixA, center),
                    Normal = math.rotate(toWorldMatrixA, norm.normalized),
                });                
            }

            return result;
        }

        //public class ConvexPolyhedron
        //{      
        //    public Face[] Faces;
        //    public Vector3[] Vertices;
        //    public Vector3[] Edges;

        //    public class Face
        //    {
        //        public Vector3 Normal;
        //        public Vector3 Vertex;
        //    }

        //    //public class Edge
        //    //{

        //    //}
        //}

        //public static bool Intersects(ConvexPolyhedron C0, ConvexPolyhedron C1)
        //{
        //    if (IsFaceSeparation(C0, C1))
        //        return false;

        //    if (IsFaceSeparation(C1, C0))
        //        return false;

        //    for (int i = 0; i < C0.Edges.Length; i++)
        //    {
        //        for (int j = 0; j < C1.Edges.Length; j++)
        //        {
        //            var edgePoint0 = C0.Edges[i];
        //            var edgePoint1 = C1.Edges[j];

        //            var axis = Vector3.Cross(edgePoint0, edgePoint1);

        //            var side0 = WhichSide(C0.Vertices, axis, edgePoint0);
        //            if (side0 == 0)
        //            {
        //                continue;
        //            }

        //            var side1 = WhichSide(C1.Vertices, axis, edgePoint0);
        //            if (side1 == 0)
        //            {
        //                continue;
        //            }

        //            if (side0 * side1 < 0)
        //            {
        //                return false;
        //            }
        //        }
        //    }
        //}

        //private static bool IsFaceSeparation(ConvexPolyhedron C0, ConvexPolyhedron C1)
        //{
        //    for (int i = 0; i < C0.Faces.Length; i++)
        //    {
        //        var normal = C0.Faces[i].Normal;
        //        var vertex = C0.Faces[i].Vertex;
        //        if (WhichSide(C1.Vertices, normal, vertex) > 0)
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        //public static int WhichSide(Vector3[] vertices, Vector3 d, Vector3 p)
        //{
        //    var positive = 0;
        //    var negative = 0;
        //    for (int i = 0; i < C.N; i++)
        //    {
        //        var t = Vector3.Dot(d, vertices[i] - p);
        //        if (t > 0) positive++;
        //        else if (t < 0) negative++;
        //        if (positive == 0 && negative == 0) return 0;
        //    }
        //    return positive == 0 ? 0 : positive > 0 ? 1 : -1;
        //}

        public static bool BoxBoxIntersects(BurstBoxCollider a, BurstBoxCollider b)
        {
            // For each Axis/Normal project and check for separation

            // Source https://gamedev.stackexchange.com/questions/44500/how-many-and-which-axes-to-use-for-3d-obb-collision-with-sat

            if (IsSeparated(ref a, ref b, a.Right))
                return false;
            if (IsSeparated(ref a, ref b, a.Up))
                return false;
            if (IsSeparated(ref a, ref b, a.Forward))
                return false;
            if (IsSeparated(ref a, ref b, b.Right))
                return false;
            if (IsSeparated(ref a, ref b, b.Up))
                return false;
            if (IsSeparated(ref a, ref b, b.Forward))
                return false;

            if (IsSeparated(ref a, ref b, Vector3.Cross(a.Right, b.Right)))
                return false;
            if (IsSeparated(ref a, ref b, Vector3.Cross(a.Right, b.Up)))
                return false;
            if (IsSeparated(ref a, ref b, Vector3.Cross(a.Right, b.Forward)))
                return false;
            if (IsSeparated(ref a, ref b, Vector3.Cross(a.Up, b.Right)))
                return false;
            if (IsSeparated(ref a, ref b, Vector3.Cross(a.Up, b.Up)))
                return false;
            if (IsSeparated(ref a, ref b, Vector3.Cross(a.Up, b.Forward)))
                return false;
            if (IsSeparated(ref a, ref b, Vector3.Cross(a.Forward, b.Right)))
                return false;
            if (IsSeparated(ref a, ref b, Vector3.Cross(a.Forward, b.Up)))
                return false;
            if (IsSeparated(ref a, ref b, Vector3.Cross(a.Forward, b.Forward)))
                return false;

            return true;
        }

        private static bool IsSeparated(ref BurstBoxCollider a, ref BurstBoxCollider b, Vector3 axis)
        {
            // Handles the cross product = {0,0,0} case
            if (axis == Vector3.zero)
                return false;

            var aMin = float.MaxValue;
            var aMax = float.MinValue;
            var bMin = float.MaxValue;
            var bMax = float.MinValue;

            // For each vertex combination >>

            CalculateMinMax(axis, a.V1, b.V1, ref aMin, ref aMax, ref bMin, ref bMax);
            CalculateMinMax(axis, a.V2, b.V2, ref aMin, ref aMax, ref bMin, ref bMax);
            CalculateMinMax(axis, a.V3, b.V3, ref aMin, ref aMax, ref bMin, ref bMax);
            CalculateMinMax(axis, a.V4, b.V4, ref aMin, ref aMax, ref bMin, ref bMax);
            CalculateMinMax(axis, a.V5, b.V5, ref aMin, ref aMax, ref bMin, ref bMax);
            CalculateMinMax(axis, a.V6, b.V6, ref aMin, ref aMax, ref bMin, ref bMax);
            CalculateMinMax(axis, a.V7, b.V7, ref aMin, ref aMax, ref bMin, ref bMax);
            CalculateMinMax(axis, a.V8, b.V8, ref aMin, ref aMax, ref bMin, ref bMax);

            // One-dimensional intersection test between a and b
            var longSpan = Mathf.Max(aMax, bMax) - Mathf.Min(aMin, bMin);
            var sumSpan = aMax - aMin + bMax - bMin;
            return longSpan >= sumSpan; // > to treat touching as intersection
        }

        private static void CalculateMinMax(Vector3 axis, Vector3 a, Vector3 b, ref float aMin, ref float aMax, ref float bMin, ref float bMax)
        {
            var aDist = Vector3.Dot(a, axis);
            aMin = aDist < aMin ? aDist : aMin;
            aMax = aDist > aMax ? aDist : aMax;
            var bDist = Vector3.Dot(b, axis);
            bMin = bDist < bMin ? bDist : bMin;
            bMax = bDist > bMax ? bDist : bMax;
        }
    }

    public struct IntersectionInfo
    {
        public int IsIntersecting;
        public Vector3 GapDirection;
        public float GapDistance;
        public Vector3 PenetrationDirection;
        public float PenetrationDistance;
        public Vector3 PointOnB;
        public Vector3 PointOnA;
    }
}
