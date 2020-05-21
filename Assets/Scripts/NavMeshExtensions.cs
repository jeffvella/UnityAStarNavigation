using System;
using System.Collections.Generic;
using System.Linq;
using BovineLabs.Entities.Helpers;
using Unity.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;

public static class NavMeshExtensions
{
    public static List<Triangle> Triangles = new List<Triangle>();

    public static List<Edge> OutsideEdges = new List<Edge>();

    private static NativeArray<Edge> _outsideEdgesNative;

    public static ref NativeArray<Edge> GetNativeEdges()
    {
        if (!_outsideEdgesNative.IsCreated)
        {
            CalculateEdges(NavMesh.CalculateTriangulation());
        }
        return ref _outsideEdgesNative;
    }

    public struct EdgeHit
    {
        public Edge Edge;
        public float Distance;
        public Vector3 Position;
        public Triangle Polygon;
        public Bool Found;
    }
    public struct Triangle
    {
        public Edge E1;
        public Edge E2;
        public Edge E3;
    }

    public struct Edge
    {
        public Vector3 Start;
        public Vector3 End;

        public Vector3 StartUp;
        public Vector3 EndUp;

        public float Length;
        public Quaternion FacingNormal;
        public Vector3 Direction;
        public Vector3 Center;

        public static Edge FromPoints(Vector3 start, Vector3 end)
        {
            return new Edge
            {
                Start = start,
                End = end,
                Center = (end + start) / 2,
                Direction = (end - start).normalized,
            };
        }

        public static readonly Edge Empty = new Edge();
    }
    public static EdgeHit FindClosestEdge(NativeArray<Edge> edges, Vector3 position)
    {
        var closestDist = float.MaxValue;
        var closestPoint = Vector3.zero;
        var closestEdge = Edge.Empty;

        foreach (var edge in edges)
        {
            var nearest = Math3d.ProjectPointOnLineSegment(edge.Start, edge.End, position);
            //var edgeDist = math.distance(nearest, position);

            // NavMesh GetTriangulation() (which this edge data is extracted from) sometimes doesn't follow the NavMesh properly
            // An example here: https://forum.unity.com/threads/navmesh-calculatetriangulation-produces-inaccurate-meshes.293894/
            // So testing distance with XY will sync the difference on horizontal plane, then we'll need to and the test
            // for vertical difference or underpass/overpass areas of the nav-mesh will give false-positives.

            var edgeDist = FastDistanceXZ(nearest, position);
            if (edgeDist < closestDist)
            {
                if (Math.Abs(nearest.y-position.y) <= 0.5f)
                {
                    closestPoint = nearest;
                    closestDist = edgeDist;
                    closestEdge = edge;
                }
            }
        }

        var result = new EdgeHit
        {
            Distance = closestDist,
            Position = closestPoint,
            Edge = closestEdge
        };

        return result;
    }

    public static float FastDistanceXZ(Vector3 a, Vector3 b)
    {
        var xD = a.x - b.x;
        var zD = a.z - b.z;
        return (xD < 0 ? -xD : xD) + (zD < 0 ? -zD : zD);
    }

    public static float FastDistance(Vector3 a, Vector3 b)
    {
        var xD = a.x - b.x;
        var yD = a.y - b.y;
        var zD = a.z - b.z;
        return (xD < 0 ? -xD : xD) + (yD < 0 ? -yD : yD) + (zD < 0 ? -zD : zD);
    }

    /// <summary>
    /// Find the position on a line that is nearest to another point.
    /// </summary>
    /// <param name="anyPointOnLine">point the line passes through</param>
    /// <param name="lineDirectionNormalized">unit vector in direction of line, either direction works</param>
    /// <param name="testPoint">the point to find nearest on line for</param>
    /// <returns></returns>
    public static Vector3 NearestPointOnLine(Vector3 testPoint, Vector3 anyPointOnLine, Vector3 lineDirectionNormalized)
    {
        var v = testPoint - anyPointOnLine;
        var d = Vector3.Dot(v, lineDirectionNormalized);
        return anyPointOnLine + lineDirectionNormalized * d;
    }

    public static void CalculateEdges(NavMeshTriangulation tr)
    {
        if(_outsideEdgesNative.IsCreated)
            _outsideEdgesNative.Dispose();

        OutsideEdges.Clear();
        Triangles.Clear();

        for (int i = 0; i < tr.indices.Length - 1; i += 3)
        {
            Vector3 v1 = tr.vertices[tr.indices[i]];
            Vector3 v2 = tr.vertices[tr.indices[i + 1]];
            Vector3 v3 = tr.vertices[tr.indices[i + 2]];

            AddOutsideEdge(v1, v2);
            AddOutsideEdge(v2, v3);
            AddOutsideEdge(v3, v1);
        }
        _outsideEdgesNative = new NativeArray<Edge>(OutsideEdges.ToArray(), Allocator.Persistent);
    }

    public static unsafe ulong ReadPolygonId(this NavMeshLocation location)
    {
        return *(ulong*)&location;
    }

    public static unsafe ulong ReadPolygonId(this PolygonId location)
    {
        return *(ulong*)&location;
    }

    private static void EnsureConnectedEdges(ICollection<Edge> edges)
    {
        var starts = new List<Vector3>();
        foreach (var edge in edges)
        {
            starts.Add(edge.Start);
        }
        foreach (var edge in edges.ToList())
        {
            if (!starts.Contains(edge.End))
                edges.Remove(edge);
        }
    }

    public static void Dispose()
    {
        if (_outsideEdgesNative.IsCreated)
        {
            _outsideEdgesNative.Dispose();
        }
    }

    private static void AddOutsideEdge( Vector3 val1, Vector3 val2)
    {
        foreach (var edge in OutsideEdges)
        {
            if (Approx(edge.Start, val1) & Approx(edge.End, val2) || Approx(edge.Start, val2) & Approx(edge.End, val1))
            {
                OutsideEdges.Remove(edge);
                return;
            }
        }
        OutsideEdges.Add(Edge.FromPoints(val1, val2));
    }

    public static bool Approx(Vector3 lhs, Vector3 rhs)
    {
        float num1 = lhs.x - rhs.x;
        float num2 = lhs.y - rhs.y;
        float num3 = lhs.z - rhs.z;
        return (double)num1 * (double)num1 + (double)num2 * (double)num2 + (double)num3 * (double)num3 < 0.01f;
    }

}