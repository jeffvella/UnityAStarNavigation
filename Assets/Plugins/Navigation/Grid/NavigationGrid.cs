using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using Grid.Algorithms;
using Navigation.Scripts.Region;
using Vella.Common.Collections;
using Vella.Common.Navigation;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Providers.Grid
{
    public sealed class NavigationGrid : GridBase, IDisposable
    {
        public float NodeBoxSize { get; set; } = 2.5f;
        private int GRID_BOUNDS { get; set; } = 50;

        public GridViewData Data { get; set; }        

        public static NavigationGrid Create(int sizeX, int sizeY, int sizeZ, Vector3 position, Quaternion rotation, Vector3 scale, float boxSize = 1f)
        {
            var len = boxSize / 2;
            var off = new Vector3(len, 0, len);                        
            var min = new Vector3(0,0,0);

            Max = new Vector3(sizeX, sizeY, sizeZ);

            var nodes = new List<GridNode>();
            for (var x = min.x + len; x < Max.x; x = x + boxSize)
            {
                for (var y = min.y + len; y < Max.y; y = y + boxSize)
                {
                    for (var z = min.z + len; z < Max.z; z = z + boxSize)
                    {
                        var navNode = new GridNode();
                        var center = new Vector3(x, y, z);
                        navNode.Center = center;
                        navNode.X = center.x;
                        navNode.Y = center.y;
                        navNode.Z = center.z;
                        navNode.Size = new Vector3(boxSize, boxSize, boxSize);
                        navNode.Bounds = new SimpleBounds(center, navNode.Size);
                        navNode.NavigableCenter = center;            

                        nodes.Add(navNode);
                    }
                }
            }

            var grid = new NavigationGrid();

            grid.InnerGrid = new NativeArray3D<GridNode>(sizeX, sizeY, sizeZ, Allocator.Persistent);
            grid.Transform.Set(position, rotation, scale);
            grid.BoxOffset = off;
            grid.BoxOffsetLength = len;
            grid.NodeBoxSize = boxSize;
            grid.Update(nodes);

            grid.Data = new GridViewData
            {
                BoxSize = boxSize,      
                Min = new int3(grid.MinX, grid.MinY, grid.MinZ),
                Max = new int3(grid.MaxX, grid.MaxY, grid.MaxZ),
            };

            return grid;
        }

        public static NavigationGrid CreateEmpty(int sizeX, int sizeY, int sizeZ, Vector3 position, Quaternion rotation, Vector3 scale, float boxSize = 1f)
        {
            var len = boxSize / 2;
            var off = new Vector3(len, 0, len);
            var min = new Vector3(0, 0, 0);

            Max = new Vector3(sizeX, sizeY, sizeZ);

            var grid = new NavigationGrid();
            grid.InnerGrid = new NativeArray3D<GridNode>(sizeX,sizeY,sizeZ, Allocator.Persistent);
            grid.Transform.Set(position, rotation, scale);
            grid.BoxOffset = off;
            grid.BoxOffsetLength = len;
            grid.NodeBoxSize = boxSize;

            grid.GridMaxX = grid.InnerGrid.GetLength(0) - 1;
            grid.GridMaxY = grid.InnerGrid.GetLength(1) - 1;
            grid.GridMaxZ = grid.InnerGrid.GetLength(2) - 1;

            grid.MinX = 0;
            grid.MaxX = grid.GridMaxX;
            grid.MinY = 0;
            grid.MaxY = grid.GridMaxY;
            grid.MinZ = 0;
            grid.MaxZ = grid.GridMaxZ;

            return grid;
        }

        public static Vector3 Max { get; set; }

        public float BoxOffsetLength { get; private set; }

        public Vector3 BoxOffset { get; private set; }

        public override float BoxSize => NodeBoxSize;

        public override int GridLength => GRID_BOUNDS;

        public override bool CanRayCast(Vector3 @from, Vector3 to)
        {
            return true;
        }

        public Vector3 GetWorldPosition(Vector3 localPosition)
        {
            return Transform.GetWorldPosition(localPosition);
        }

        public Vector3 GetLocalPosition(Vector3 worldPosition)
        {
            return Transform.GetLocalPosition(worldPosition);
        }

        public ref GridNode GetNearestByRef(GridPoint point)
        {
            return ref GetNearestNodeRef(point.X, point.Y, point.Z);
        }

        public override bool CanRayWalk(Vector3 from, Vector3 to)
        {
            var nodes = GetRayLine(from, to);
            var canWalk = nodes.All(point =>
            {
                var node = GetNearestNode(point);
                return !node.Equals(default) && node.HasFlag(NodeFlags.AllowWalk);
            });
            return canWalk;
        }

        public IEnumerable<GridPoint> GetRayLine(Vector3 from, Vector3 to, float limitDistance = 0)
        {
            if (limitDistance > 0)
            {
                to = (to - from) * limitDistance;
            }

            var gridFrom = ToGridPoint(Transform.GetLocalPosition(from));
            var gridTo = ToGridPoint(Transform.GetLocalPosition(to));

            return Bresenham.GetPointsOnLine(gridFrom.X, gridFrom.Y, gridFrom.Z, gridTo.X, gridTo.Y, gridTo.Z)
                .Select(p => new GridPoint((int)p.x,(int)p.y,(int)p.z));
        }

        public bool CanLocalRayWalk(GridPoint from, GridPoint to, ulong flags)
        {
            var nodes = GetLocalRayLine(from, to);
            var canWalk = nodes.All(point =>
            {
                var node = InnerGrid[point.X, point.Y, point.Z];
                return !node.Equals(default) && node.HasFlag((NodeFlags)flags);
            });
            return canWalk;
        }

        public IEnumerable<GridPoint> GetLocalRayLine(GridPoint start, GridPoint end, float limitDistance = 0)
        {
            return Bresenham.GetPointsOnLine(start.X, start.Y, start.Z, end.X, end.Y, end.Z)
                .Select(p => new GridPoint((int)p.x, (int)p.y, (int)p.z));
        }

        public Bounds LocalDataBounds
        {
            get
            {
                var size = new Vector3(MaxX, MaxY, MaxZ);
                return new Bounds(Round(size / 2f), size);
            }
        }

        public GridNode FindClosestNode(Vector3 worldPosition, NodeFlags flags = default, int maxDistance = 10)
        {
            var localPosition = Transform.GetLocalPosition(worldPosition);
            var nearestNodeInfo = TryGetNearestNodeInfo(localPosition);

            if (!nearestNodeInfo.success)
            {
                return SearchNodes(nearestNodeInfo.GridPoint, maxDistance).FirstOrDefault(n => n.HasFlag(flags));
            }

            return flags == NodeFlags.None || nearestNodeInfo.Node.HasFlag(flags) 
                ? nearestNodeInfo.Node
                : SearchNodes(nearestNodeInfo.GridPoint, maxDistance).FirstOrDefault(n => n.HasFlag(flags));
        }

        public GridNode FindClosestNode(Vector3 worldPosition, ulong allowFlags, int maxDistance = 10)
        {
            return FindClosestNode(worldPosition, (NodeFlags) allowFlags, maxDistance);
        }

        public GridNode FindClosestNode(GridNode node, NodeFlags flags = default, int maxDistance = 10)
        {
            var nearestNodeInfo = TryGetNearestNodeInfo(node.NavigableCenter);

            if (!nearestNodeInfo.success)
            {
                return SearchNodes(nearestNodeInfo.GridPoint, maxDistance).FirstOrDefault(n => n.HasFlag(flags));
            }

            return flags == NodeFlags.None || nearestNodeInfo.Node.HasFlag(flags)
                ? nearestNodeInfo.Node             
                : SearchNodes(nearestNodeInfo.GridPoint, maxDistance).FirstOrDefault(n => n.HasFlag(flags));
        }

        public (bool isNodeOutside, bool isNodeInside) GetCollisionInfo(Collider collider, GridNode node)
        {
            var nodeWorldPos = ToWorldPosition(node.Center);
            var distance = Vector3.Distance(collider.ClosestPoint(nodeWorldPos), nodeWorldPos);
            return (distance < node.Bounds.Extents.x, distance <= 0);
        }

        public (bool isNodeOutside, bool isNodeInside) GetCollisionInfo(Collider collider, Vector3 worldPosition, float radius)
        {    
            var distance = Vector3.Distance(collider.ClosestPoint(worldPosition), worldPosition);
            return (distance < radius, distance <= 0);
        }

        public (bool isNodeOutside, bool isNodeInside) GetNavigableCollisionInfo(Collider collider, GridNode node, float radius)
        {       
            var distance = Vector3.Distance(collider.ClosestPoint(node.NavigableCenter), node.NavigableCenter);
            return (distance < radius, distance <= 0);
        }


        public bool NodeIsOutside(Collider collider, GridNode node)
        {
            var nodeWorldPos = ToWorldPosition(node.Center);
            return Vector3.Distance(collider.ClosestPoint(nodeWorldPos), nodeWorldPos) < node.Bounds.Extents.x;
        }

        public bool NodeIsInside(Collider collider, GridNode node)
        {
            var nodeWorldPos = ToWorldPosition(node.Center);
            return Vector3.Distance(collider.ClosestPoint(nodeWorldPos), nodeWorldPos) < node.Bounds.Extents.x;
        }

        public bool CollidesWithNode(Collider collider, GridNode node, out float gapDistance)
        {
            var nodeWorldPos = ToWorldPosition(node.Center);
            gapDistance = (int)Vector3.Distance(collider.bounds.center, nodeWorldPos);
            var closestPoint = collider.ClosestPoint(nodeWorldPos);
            return Vector3.Distance(closestPoint, nodeWorldPos) < node.Bounds.Extents.x;
        }

        public bool CollidesWithNode(Collider collider, GridNode node, out float gapDistance, out Vector3 pointOnCollider)
        {
            var nodeWorldPos = ToWorldPosition(node.Center);
            gapDistance = (int)Vector3.Distance(collider.bounds.center, nodeWorldPos);
            pointOnCollider = collider.ClosestPoint(nodeWorldPos);
            return Vector3.Distance(pointOnCollider, nodeWorldPos) < node.Bounds.Extents.x;
        }

        public int NodeCount => MaxX * MaxY * MaxZ;

        public void Dispose()
        {
            if (InnerGrid.IsCreated)
            {
                InnerGrid.Dispose();
            }
        }


    }



}