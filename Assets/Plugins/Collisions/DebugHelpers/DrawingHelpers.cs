using UnityEngine;
using Vella.SimpleBurstCollision;

public static class DrawingHelpers
{
    public static void DrawVector(Vector3 origin, Vector3 vector, float size = 0.02f)
    {
        var halfSize = size * 0.5f;
        var end = origin + vector;
        Gizmos.DrawLine(origin, end);
        Gizmos.DrawSphere(origin, size);

        var arrowOffset = end - vector.normalized * size;
        var up = Vector3.Cross(vector, Vector3.up) * size;
        var down = Vector3.Cross(vector, Vector3.down) * size;
        var left = Vector3.Cross(vector, Vector3.left) * size;
        var right = Vector3.Cross(vector, Vector3.right) * size;

        var a1 = arrowOffset + up;
        var a2 = arrowOffset + up + Vector3.Cross(vector, Vector3.left) * halfSize;
        var a3 = arrowOffset + up + Vector3.Cross(vector, Vector3.right) * halfSize;
        var a4 = arrowOffset + down;
        var a5 = arrowOffset + down + Vector3.Cross(vector, Vector3.left) * halfSize;
        var a6 = arrowOffset + down + Vector3.Cross(vector, Vector3.right) * halfSize;
        var a7 = arrowOffset + left;
        var a8 = arrowOffset + right;

        Gizmos.DrawLine(end, a2);
        Gizmos.DrawLine(end, a3);
        Gizmos.DrawLine(end, a5);
        Gizmos.DrawLine(end, a6);
        Gizmos.DrawLine(end, a7);
        Gizmos.DrawLine(end, a8);

        Gizmos.DrawLine(a1, a2);
        Gizmos.DrawLine(a1, a3);
        Gizmos.DrawLine(a4, a5);
        Gizmos.DrawLine(a4, a6);
        Gizmos.DrawLine(a8, a6);
        Gizmos.DrawLine(a8, a3);
        Gizmos.DrawLine(a7, a2);
        Gizmos.DrawLine(a7, a5);

    }

    public static void DrawWireFrame(BurstBoxCollider obb)
    {
        Gizmos.DrawLine(obb.V1, obb.V3);
        Gizmos.DrawLine(obb.V1, obb.V2);
        Gizmos.DrawLine(obb.V3, obb.V4);
        Gizmos.DrawLine(obb.V2, obb.V4);
        Gizmos.DrawLine(obb.V1, obb.V5);
        Gizmos.DrawLine(obb.V2, obb.V6);
        Gizmos.DrawLine(obb.V3, obb.V7);
        Gizmos.DrawLine(obb.V4, obb.V8);
        Gizmos.DrawLine(obb.V5, obb.V7);
        Gizmos.DrawLine(obb.V5, obb.V6);
        Gizmos.DrawLine(obb.V7, obb.V8);
        Gizmos.DrawLine(obb.V8, obb.V6);
    }
}