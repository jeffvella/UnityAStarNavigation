using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Priority_Queue;
using Providers.Grid;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = System.Random;

namespace Vella.Common.Navigation
{
    public enum PathStatus
    {
        None = 0,
        Complete,
        Partial
    }

    public class BurstAStarPathFinder
    {
        public NavigationGrid Grid;

        private readonly Random _random = new Random();

        public PathStatus Status = PathStatus.Partial;

        public int Id;

        public BurstAStarPathFinder(NavigationGrid grid)
        {
            Grid = grid;
        }

        public Stopwatch Stopwatch { get; } = new Stopwatch();

        public INativeArrayProducer<NativeAreaDefinition> Areas { get; set; }

        public ulong AllowFlags { get; set; }

        public List<GridNode> NodePath { get; set; } = new List<GridNode>();
        public List<Vector3> Path { get; set; } = new List<Vector3>();

        public void Clear()
        {      
            NodePath?.Clear();
            Path?.Clear();            
        }
       
        public BurstAStarPathFinder GetPath(Vector3 from, Vector3 to)
        {
            var startNode = Grid.FindClosestNode(from, AllowFlags, 20);
            var endNode = Grid.FindClosestNode(to, AllowFlags, 20);
            return GetPath(startNode, endNode);
        }

        public BurstAStarPathFinder GetPath(GridNode from, GridNode to)
        {
            Id = _random.Next(1, int.MaxValue);

            var jobResult = PathFindingJob.Complete(this,
                ref Grid.InnerGrid, Areas, Id,
                from.GridPoint, to.GridPoint, 
                AllowFlags, Grid.Transform.ToWorldMatrix);

            Status = jobResult.PathStatus;
            NodePath = jobResult.NodePath;
            Path = jobResult.VectorPath;
            return this;
        }

        [BurstCompile]
        public struct PathFindingJob : IJob
        {
            public struct PathFindingJobResult
            {
                public PathStatus PathStatus;               
            }

            public NativePriorityQueue OpenQueue;
            public NativeArray3D<GridNode> Grid;
            public NativeArray<NativeAreaDefinition> Areas;
            public float4x4 TransformMatrix;
            public GridPoint StartPoint;
            public GridPoint EndPoint;
            public NativeList<GridNode> Path;
            public NativeList<Vector3> WorldPath;
            public ulong AllowedFlags;
            public int QueryId;
            public int MaxPoints;

            [NativeDisableUnsafePtrRestriction]
            public unsafe PathFindingJobResult* ResultPtr;

            public static unsafe (PathStatus PathStatus, List<GridNode> NodePath, List<Vector3> VectorPath) Complete(BurstAStarPathFinder pathFinder, ref NativeArray3D<GridNode> grid,
                INativeArrayProducer<NativeAreaDefinition> areas, int queryId, GridPoint start, GridPoint end, ulong allowedFlags, float4x4 localToWorld)
            {
                using (var nativeAreas = areas.ToNativeArray(Allocator.TempJob))
                using (var nativePath = new NativeList<GridNode>(grid.Length, Allocator.TempJob))
                using (var nativeVectorPath = new NativeList<Vector3>(grid.Length, Allocator.TempJob))
                using (var nativePriorityQueue = new NativePriorityQueue(grid.Length / 8, Allocator.TempJob))
                {
                    var result = new PathFindingJobResult();
                    var job = new PathFindingJob
                    {
                        QueryId = queryId,
                        AllowedFlags = allowedFlags,
                        StartPoint = start,
                        EndPoint = end,
                        OpenQueue = nativePriorityQueue,
                        Grid = grid,
                        Areas = nativeAreas,
                        Path = nativePath,
                        MaxPoints = 500,
                        WorldPath = nativeVectorPath,
                        TransformMatrix = localToWorld,
                        ResultPtr = &result
                    };

                    pathFinder.Stopwatch.Restart();
                    job.Run();
                    pathFinder.Stopwatch.Stop();

                    var path = job.Path.ToArray().ToList();
                    var worldPath = nativeVectorPath.ToArray().ToList();
                    return (result.PathStatus, path, worldPath);
                }
            }

            public unsafe void Execute()
            {
                ref var result = ref ResultPtr;
                ref var start = ref Grid.AsRef(StartPoint.X, StartPoint.Y, StartPoint.Z);
                ref var end = ref Grid.AsRef(EndPoint.X, EndPoint.Y, EndPoint.Z);

                OpenQueue.Enqueue(start.GridPoint, 0);

                start.OpenId = QueryId;

                //var eX = end.NavigableCenter.x;
                //var eY = end.NavigableCenter.y;
                //var eZ = end.NavigableCenter.z;

                var closest = start;

                var maxX = Grid.GetLength(0) - 1;
                var maxY = Grid.GetLength(1) - 1;
                var maxZ = Grid.GetLength(2) - 1;

                while (OpenQueue.Count > 0)
                {
                    var g = OpenQueue.Dequeue();
                    ref var current = ref Grid.AsRef(g.X, g.Y, g.Z);

                    current.OpenId = QueryId;
                    current.ClosedId = QueryId;

                    if (current.GridPoint == EndPoint)
                    {
                        result->PathStatus = PathStatus.Complete;
                        RetracePath(ref start, ref current);
                        return;
                    }

                    //var cX = current.NavigableCenter.x;
                    //var cY = current.NavigableCenter.y;
                    //var cZ = current.NavigableCenter.z;

                    var currentPoint = current.GridPoint;

                    var xMin = currentPoint.X - 1;
                    var yMin = currentPoint.Y - 1;
                    var zMin = currentPoint.Z - 1;
                    var xMax = currentPoint.X + 1;
                    var yMax = currentPoint.Y + 1;
                    var zMax = currentPoint.Z + 1;
                    if (xMin < 0) xMin = 0;
                    if (yMin < 0) yMin = 0;
                    if (zMin < 0) zMin = 0;
                    if (xMax > maxX) xMax = maxX;
                    if (yMax > maxY) yMax = maxY;
                    if (zMax > maxZ) zMax = maxZ;

                    for (var x = xMin; x <= xMax; x++)
                    {
                        for (var y = yMin; y <= yMax; y++)
                        {
                            for (var z = zMin; z <= zMax; z++)
                            {
                                if (x == currentPoint.X && y == currentPoint.Y && z == currentPoint.Z)
                                    continue;

                                ref var neighbor = ref Grid.AsRef(x, y, z);

                                if ((neighbor.Flags & AllowedFlags) == 0)
                                    continue;

                                if (neighbor.ClosedId == QueryId)
                                    continue;

                                //if (Math.Abs(neighbor.Y-current.Y) > 0.8f)
                                //    continue;                                

                                var nX = neighbor.NavigableCenter.x;
                                var nY = neighbor.NavigableCenter.y;
                                var nZ = neighbor.NavigableCenter.z;

                                var distance = math.distance(current.NavigableCenter, neighbor.NavigableCenter); //GetDistance(cX, cY, cZ, nX, nY, nZ);

                                float areaModifier = 0;
                                for (var j = 0; j < Areas.Length; j++)
                                {
                                    areaModifier += (current.Flags & Areas[j].FlagValue) != 0 ? Areas[j].Weight * distance : 0;
                                }

                                var newCostToNeighbour = current.GScore + distance - areaModifier;
                                var isOpen = neighbor.OpenId == QueryId;

                                if (newCostToNeighbour < neighbor.GScore || !isOpen)
                                {
                                    neighbor.GScore = newCostToNeighbour;
                                    var h = math.distance(neighbor.NavigableCenter, end.NavigableCenter); //GetDistance(nX, nY, nZ, eX, eY, eZ);

                                    neighbor.HScore = h;
                                    neighbor.FScore = newCostToNeighbour + h;
                                    neighbor.ParentPoint = current.GridPoint;

                                    if (closest.HScore <= 0 || h < closest.HScore)
                                    {
                                        closest = neighbor;
                                    }

                                    if (!isOpen)
                                    {
                                        OpenQueue.Enqueue(neighbor.GridPoint, neighbor.FScore);
                                        neighbor.OpenId = QueryId;
                                    }
                                }
                            }
                        }
                    }
                }

                if (closest != start)
                {
                    result->PathStatus = PathStatus.Partial;
                    RetracePath(ref start, ref closest);
                }
            }

            private float GetDistance(float aX, float aY, float aZ, float bX, float bY, float bZ)
            {
                var xD = aX - bX;
                var yD = aY - bY;
                var zD = aZ - bZ;
                return (xD < 0 ? -xD : xD) + (yD < 0 ? -yD : yD) + (zD < 0 ? -zD : zD);
            }

            private void RetracePath(ref GridNode startNode, ref GridNode endNode)
            {
                ref var currentNode = ref endNode;
                var i = 0;

                while (currentNode.GridPoint != startNode.GridPoint && i < MaxPoints)
                {
                    Path.Add(currentNode);
                    currentNode = ref Grid.AsRef(currentNode.ParentPoint.X, currentNode.ParentPoint.Y, currentNode.ParentPoint.Z);
                    i++;
                }

                Path.Add(startNode);
                Reverse(ref Path, 0, Path.Length);

                if (!TransformMatrix.Equals(float4x4.zero))
                {
                    for (var j = 0; j < Path.Length; j++)
                    {
                        WorldPath.Add(math.transform(TransformMatrix, Path[j].NavigableCenter));
                    }
                }
            }

            public static void Reverse<T>(ref NativeList<T> array, int index, int length) where T : struct
            {
                if (!array.IsCreated || array.Length <= 0)
                    return;

                var maxIndex = array.Length - 1;
                var i = index;
                var j = index + length - 1;

                if (i > maxIndex || j > maxIndex)
                    return;

                while (i < j)
                {
                    var temp = array[i];
                    array[i] = array[j];
                    array[j] = temp;
                    i++;
                    j--;
                }
            }

        }

    }
}