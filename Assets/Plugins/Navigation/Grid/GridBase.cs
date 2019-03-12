using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Providers.Grid;
using Vella.Common.Navigation;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.XR.WSA.Input;

namespace Providers.Grid
{
    public struct GridViewData
    {
        public float BoxSize;
        public int3 Min;
        public int3 Max;
    }

    public delegate void UpdatedEventHandler(object sender, List<GridNode> newNodes);
    
    public abstract class GridBase
    {
        protected GridBase()
        {
            Id = Guid.NewGuid();
        }

        public readonly Guid Id;

        public event UpdatedEventHandler Updated;

        public NativeArray3D<GridNode> InnerGrid;

        public abstract float BoxSize { get; }

        public abstract int GridLength { get; }


        public int MinX = int.MaxValue;
        public int MaxX = 0;
        public int MinY = int.MaxValue;
        public int MaxY = 0;
        public int MinZ = int.MaxValue;
        public int MaxZ = 0;

        internal int GridMaxX;
        internal int GridMaxY;
        internal int GridMaxZ;

        public SimpleTransform Transform { get; set; } = new SimpleTransform();

        public abstract bool CanRayCast(Vector3 @from, Vector3 to);

        public abstract bool CanRayWalk(Vector3 @from, Vector3 to);

        protected virtual void OnUpdated(List<GridNode> nodes)
        {
            Updated?.Invoke(this, nodes);
        }
    
        public static Vector3 Round(Vector3 a, MidpointRounding rounding = MidpointRounding.AwayFromZero)
        {
            return new Vector3((float)Math.Round(a.x, rounding), (float)Math.Round(a.y, rounding), (float)Math.Round(a.z, rounding));
        }

        public void Update(List<GridNode> nodes)
        {
            //Debug.LogFormat("[{0}] Updating grid with {1} new nodes", GetType().Name, nodes.Count);

            nodes = nodes.OrderBy(n => n.Center.x).ThenBy(n => n.Center.y).ToList();

            for (var index = 0; index < nodes.Count; index++)
            {
                var node = nodes[index];
                
                // Snap the precise position to the nearest grid point.
                var nodeX = ToGridDistance(node.Center.x);
                var nodeY = ToGridDistance(node.Center.y);
                var nodeZ = ToGridDistance(node.Center.z);

                //var existing = InnerGrid[nodeX, nodeY, nodeZ];
                //if (existing.Equals(default(IGridNode)))
                //{
                    node.GridPoint = new GridPoint(nodeX, nodeY, nodeZ);
                    InnerGrid[nodeX, nodeY, nodeZ] = node;
                 
                //}
                //else
                //{
                //    nodes[index] = existing;
                //}

                // Keep expanding bounds to encompass nodes seen.
                if (MinX > nodeX) MinX = nodeX;
                if (MaxX < nodeX) MaxX = nodeX;
                if (MinY > nodeY) MinY = nodeY;
                if (MaxY < nodeY) MaxY = nodeY;
                if (MinZ > nodeZ) MinZ = nodeZ;
                if (MaxZ < nodeZ) MaxZ = nodeZ;
            }

            //for (var index0 = 0; index0 < InnerGrid.GetLength(0); index0++)
            //for (var index1 = 0; index1 < InnerGrid.GetLength(1); index1++)
            //for (var index2 = 0; index2 < InnerGrid.GetLength(2); index2++)
            //{
            //    ref var node = ref InnerGrid[index0, index1, index2];
            //    var i = 0;
            //    var gp = node.GridPoint;
            //    var xMin = gp.x - 1;
            //    var yMin = gp.y - 1;
            //    var zMin = gp.z - 1;
            //    var xMax = gp.x + 1;
            //    var yMax = gp.y + 1;
            //    var zMax = gp.z + 1;
            //    if (xMin < 0) xMin = 0;
            //    if (yMin < 0) yMin = 0;
            //    if (zMin < 0) zMin = 0;
            //    if (xMax > MaxX) xMax = MaxX;
            //    if (yMax > MaxY) yMax = MaxY;
            //    if (zMax > MaxZ) zMax = MaxZ;
            //    for (var x = xMin; x <= xMax; x++)
            //    for (var y = yMin; y <= yMax; y++)
            //    for (var z = zMin; z <= zMax; z++)
            //    {
            //        if (x == gp.x && y == gp.y && z == gp.z)
            //            continue;

            //        node.Neighbors[i] = new GridPoint(x,y,z);
            //        i++;
            //    }
            //}

            GridMaxX = InnerGrid.GetLength(0)-1;
            GridMaxY = InnerGrid.GetLength(1)-1;
            GridMaxZ = InnerGrid.GetLength(2)-1;

            //BaseSize = (int)Math.Round(BoxSize / 4, MidpointRounding.AwayFromZero);
            OnUpdated(nodes);
        }

        public GridNode GetNearestNode(GridPoint gridPoint)
        {
            return GetNearestNode(gridPoint.X, gridPoint.Y, gridPoint.Z);
        }

        public GridNode GetNearestNode(Vector3 position)
        {
            return GetNearestNode(ToGridDistance(position.x), ToGridDistance(position.y), ToGridDistance(position.z));
        }


        public (bool success, GridNode Node, GridPoint GridPoint) TryGetNearestNodeInfo(Vector3 position)
        {
            var x = ToGridDistance(position.x);
            var y = ToGridDistance(position.y);
            var z = ToGridDistance(position.z);
            var node = GetNearestNode(x, y, z);
            var isNull = node.Equals(default);
            return (!isNull, node, new GridPoint(x,y,z));
        }    

        public GridNode GetNearestNode(int gridX, int gridY, int gridZ)
        {
            if (gridX < 0) gridX = 0;
            if (gridY < 0) gridY = 0;
            if (gridZ < 0) gridZ = 0;
            if (gridX > MaxX) gridX = MaxX;
            if (gridY > MaxY) gridY = MaxY;
            if (gridZ > MaxZ) gridZ = MaxZ;
            return InnerGrid[gridX, gridY, gridZ];
        }

        public ref GridNode GetNearestNodeRef(int gridX, int gridY, int gridZ)
        {
            if (gridX < 0) gridX = 0;
            if (gridY < 0) gridY = 0;
            if (gridZ < 0) gridZ = 0;
            if (gridX > MaxX) gridX = MaxX;
            if (gridY > MaxY) gridY = MaxY;
            if (gridZ > MaxZ) gridZ = MaxZ;
            return ref InnerGrid.AsRef(gridX, gridY, gridZ);
        }

        public bool IsInsideBounds(GridPoint gridPoint)
        {
            return IsInsideBounds(gridPoint.X, gridPoint.Y, gridPoint.Z);
        }

        public bool IsInsideBounds(int gridX, int gridY, int gridZ)
        {
            return gridX >= 0 && gridY >= 0 && gridZ >= 0 && gridX < MaxX && gridY < MaxY && gridZ < MaxZ;
        }

        public bool IsWalkable(GridPoint gridPoint)
        {  
            return IsWalkable(gridPoint.X, gridPoint.Y, gridPoint.Z);
        }

        public bool IsWalkable(int gridX, int gridY, int gridZ)
        {
            return IsInsideBounds(gridX, gridY, gridZ) && InnerGrid[gridX, gridY, gridZ].IsWalkable;
        }


        public int3 ToGridDistance(Vector3 value)
        {
            return new int3(ToGridDistance(value.x),ToGridDistance(value.y), ToGridDistance(value.z));
        }

        public int ToGridDistance(float value)
        {
            return (int)Math.Round((value - (BoxSize / 2)) / BoxSize, MidpointRounding.AwayFromZero);
        }


        public Vector3 ToWorldPosition(Vector3 localPosition)
        {
            return Transform.GetWorldPosition(localPosition);
        }

        public List<GridNode> GetNeighbors(int originX, int originY, int originZ, int distance = 1)
        {
            var neighbors = new List<GridNode>();

            for (var x = originX - distance; x <= originX + distance; x++)
            {
                if (x < 0 || x > MaxX) continue;

                for (var y = originY - distance; y <= originY + distance; y++)
                {
                    if (y < 0 || y > MaxY) continue;

                    for (var z = originZ - distance; z <= originZ + distance; z++)
                    {
                        if (z < 0 || z > MaxZ) continue;

                        if (x == originX && y == originY && z == originZ)
                            continue;

                        var node = (GridNode)InnerGrid[x, y, z];
                        if (!node.Equals(default))
                        {
                            neighbors.Add(node);                          
                        }

                    }
                }
            }
            return neighbors;
        }

        public GridNode[] GetNeighborsUnsafe(int originX, int originY, int originZ, int distance = 1)
        {
            var arr = new GridNode[27];
            var xMin = originX - distance;
            var yMin = originY - distance;
            var zMin = originZ - distance;
            var xMax = originX + distance;
            var yMax = originY + distance;
            var zMax = originZ + distance;

            if (xMin < 0) xMin = 0;
            if (yMin < 0) yMin = 0;
            if (zMin < 0) zMin = 0;
            if (xMax > MaxX) xMax = MaxX;
            if (yMax > MaxY) yMax = MaxY;
            if (zMax > MaxZ) zMax = MaxZ;

            var i = 0;
            for (var x = xMin; x <= xMax; x++)
            {
                for (var y = yMin; y <= yMax; y++)
                {
                    for (var z = zMin; z <= zMax; z++)
                    {
                        if (x == originX && y == originY && z == originZ)
                            continue;

                        arr[i] = InnerGrid[x, y, z];
                        //yield return InnerGrid[x, y, z];          
                        i++;
                    }
                }
            }

            return arr;
        }

        /// <summary>
        /// Returns nodes in layers, starting from the closest ones. (Prioritizing horizontal plane before searching vertically)
        /// </summary>
        /// <param name="origin">the starting node.</param>
        /// <param name="maxDistance">how far the search go (in nodes) from the starting node.</param>
        /// <returns></returns>
        public IEnumerable<GridNode> FindNodes(Vector3 origin, int maxDistance = 10)
        {
            var originX = (int)origin.x;
            var originY = (int)origin.y;
            var originZ = (int)origin.z;

            for (int i = 1; i <= maxDistance; i++)
            {
                for (var y = originY - i; y <= originY + i; y++)
                {
                    if (y < 0 || y > MaxY) continue;

                    for (var x = originX - i; x <= originX + i; x++)
                    {
                        if (x < 0 || x > MaxX) continue;
                        
                        for (var z = originZ - i; z <= originZ + i; z++)
                        {
                            if (z < 0 || z > MaxZ) continue;

                            // Excluding itself
                            if (x == originX && y == originY && z == originZ)
                                continue;

                            // Discard inner layers
                            var innerCavity = i - 1; 
                            if (x <= originX + innerCavity && y <= originY + innerCavity && z <= originZ + innerCavity && x >= originX - innerCavity && y >= originY - innerCavity && z >= originZ - innerCavity)
                                continue;

                            var node = (GridNode)InnerGrid[x, y, z];
                            if (!node.Equals(default))
                            {
                                yield return node;
                            }

                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns nodes in layers, starting from the closest ones. (Prioritizing horizontal plane before searching vertically)
        /// </summary>
        /// <param name="origin">the starting node.</param>
        /// <param name="maxDistance">how far the search go (in nodes) from the starting node.</param>
        /// <returns></returns>
        public IEnumerable<GridNode> SearchNodes(GridPoint origin, int maxDistance)
        {
            var originX = (int)origin.X;
            var originY = (int)origin.Y;
            var originZ = (int)origin.Z;

            for (int i = 1; i <= maxDistance; i++)
            {
                for (var y = originY - i; y <= originY + i; y++)
                {
                    if (y < 0 || y > MaxY) continue;

                    for (var x = originX - i; x <= originX + i; x++)
                    {
                        if (x < 0 || x > MaxX) continue;

                        for (var z = originZ - i; z <= originZ + i; z++)
                        {
                            if (z < 0 || z > MaxZ) continue;

                            // Excluding itself
                            if (x == originX && y == originY && z == originZ)
                                continue;

                            // Discard inner layers
                            var innerCavity = i - 1;
                            if (x <= originX + innerCavity 
                             && y <= originY + innerCavity                                                            
                             && z <= originZ + innerCavity 
                             && x >= originX - innerCavity 
                             && y >= originY - innerCavity 
                             && z >= originZ - innerCavity)
                                continue;

                            var node = (GridNode)InnerGrid[x, y, z];  
                            if (!node.Equals(default))
                            {
                                yield return node;
                            }

                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns nodes in layers, starting from the closest ones. (Prioritizing horizontal plane before searching vertically)
        /// </summary>
        /// <param name="origin">the starting node.</param>
        /// <param name="maxDistance">how far the search go (in nodes) from the starting node.</param>
        /// <returns></returns>
        public IEnumerable<GridNode> SearchNodesBlock(GridPoint origin, int maxDistance)
        {
            var originX = (int)origin.X;
            var originY = (int)origin.Y;
            var originZ = (int)origin.Z;

            for (int i = 1; i <= maxDistance; i++)
            {
                for (var y = originY - i; y <= originY + i; y++)
                {
                    if (y < 0 || y > MaxY) continue;

                    for (var x = originX - i; x <= originX + i; x++)
                    {
                        if (x < 0 || x > MaxX) continue;

                        for (var z = originZ - i; z <= originZ + i; z++)
                        {
                            if (z < 0 || z > MaxZ) continue;

                            // Excluding itself
                            if (x == originX && y == originY && z == originZ)
                                continue;

                            // Discard inner layers
                            var innerCavity = i - 1;
                            if (x <= originX + innerCavity
                             && y <= originY + innerCavity
                             && z <= originZ + innerCavity
                             && x >= originX - innerCavity
                             && y >= originY - innerCavity
                             && z >= originZ - innerCavity)
                                continue;

                            var node = (GridNode)InnerGrid[x, y, z];
                            if (!node.Equals(default))
                            {
                                yield return node;
                            }

                        }
                    }
                }
            }
        }


        public IEnumerable<GridNode> EnumerateNodeRange(int3 min, int3 max)
        {
            for (var x = min.x; x < max.x; x++)
            {
                for (var y = min.y; y < max.y; y++)
                {
                    for (var z = min.z; z < max.z; z++)
                    {
                        yield return InnerGrid[x, y, z];
                    }
                }
            }
        }

   

        /// <summary>
        /// Returns nodes in layers, starting from the closest ones. (Prioritizing horizontal plane before searching vertically)
        /// </summary>
        /// <param name="origin">the starting node.</param>
        /// <param name="maxDistance">how far the search go (in nodes) from the starting node.</param>
        /// <returns></returns>
        public IEnumerable<(GridNode node, int layer)> SearchNodes2(GridPoint origin, int maxDistance = 10)
        {
            var originX = (int)origin.X;
            var originY = (int)origin.Y;
            var originZ = (int)origin.Z;

            for (int i = 1; i <= maxDistance; i++)
            {
                for (var y = originY - i; y <= originY + i; y++)
                {
                    if (y < 0 || y > MaxY) continue;

                    for (var x = originX - i; x <= originX + i; x++)
                    {
                        if (x < 0 || x > MaxX) continue;

                        for (var z = originZ - i; z <= originZ + i; z++)
                        {
                            if (z < 0 || z > MaxZ) continue;

                            // Excluding itself
                            if (x == originX && y == originY && z == originZ)
                                continue;

                            // Discard inner layers
                            var innerCavity = i - 1;
                            if (x <= originX + innerCavity && y <= originY + innerCavity && z <= originZ + innerCavity && x >= originX - innerCavity && y >= originY - innerCavity && z >= originZ - innerCavity)
                                continue;

                            var node = (GridNode)InnerGrid[x, y, z];
                            if (!node.Equals(default))
                            {
                                yield return (node, i);
                            }

                        }
                    }
                }
            }
        }


        public List<GridNode> GetNeighbors2(Vector3 origin, Vector3 heading, int distance = 1)
        {
            var originX = (int)origin.x;
            var originY = (int)origin.y;
            var originZ = (int)origin.z;

            var neighbors = new List<GridNode>();

            var maxX = MaxX;
            var maxY = MaxY;
            var maxZ = MaxZ;

            void AddNode(int x, int y, int z)
            {
                var node = InnerGrid[x, y, z];
                if (node.Equals(default))
                {
                    neighbors.Add((GridNode)node);
                }
            }


            IEnumerable<int> Loop(int start, int offset, bool direction)
            {
                if (direction)
                {
                    for (var x = start - offset; x <= start + offset; x++)
                    {
                        yield return x;
                    }
                }
                else
                {
                    for (var x = start + offset; x >= start - offset; x--)
                    {
                        yield return x;
                    }
                }
            }

            var axisX = (Axis: Axis.X, Heading: heading.x, Origin: originX);
            var axisY = (Axis: Axis.Y, Heading: heading.y, Origin: originY);
            var axisZ = (Axis: Axis.Z, Heading: heading.z, Origin: originZ);

            var axisDominance = new[] { axisX, axisY, axisZ };

            var orderedAxis = axisDominance.OrderByDescending(a => Math.Abs(a.Heading)).ToArray();

            var axis1Info = orderedAxis[0];
            var axis2Info = orderedAxis[1];
            var axis3Info = orderedAxis[2];

            foreach (var axis1 in Loop(axis1Info.Origin, distance, axis3Info.Heading <= 0))
            {      
 
                foreach (var axis2 in Loop(axis2Info.Origin, distance, axis3Info.Heading <= 0))
                {
      
                    foreach (var axis3 in Loop(axis3Info.Origin, distance, axis3Info.Heading <= 0))
                    {
                        if (axis1Info.Axis == Axis.X)
                        {
                            if (axis2Info.Axis == Axis.Y)
                            {                   
                                AddNode(axis1, axis2, axis3);                               
                            }
                            else // axis2 = z
                            {
                                AddNode(axis1, axis3, axis2);
                            }                  
                        }
                        else if (axis1Info.Axis == Axis.Y)
                        {
                            if (axis2Info.Axis == Axis.X)
                            {
                                AddNode(axis2, axis1, axis3);
                            }
                            else // axis2 = z
                            {
                                AddNode(axis3, axis1, axis2);
                            }
                        }
                        else // axis1 == z
                        {
                            if (axis2Info.Axis == Axis.X)
                            {
                                AddNode(axis2, axis3, axis1);
                            }
                            else // axis2 = y
                            {
                                AddNode(axis3, axis2, axis1);
                            }
                        }


                    }
                }

            }
   
            return neighbors;
        }

        public GridPoint ToGridPoint(Vector3 localPosition)
        {
            var x = ToGridDistance(localPosition.x);
            var y = ToGridDistance(localPosition.y);
            var z = ToGridDistance(localPosition.z);
            return new GridPoint(x, y, z);
        }


    }
}