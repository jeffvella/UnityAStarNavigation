using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Unity.Mathematics;
using UnityEngine;

namespace Providers.Grid
{        
    public struct GridPoint : IEquatable<GridPoint>
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;
        public int QueueIndex;
        public float Priority;

        public GridPoint(int x, int y, int z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.QueueIndex = -1;
            this.Priority = -1;
        }

        public bool Equals(GridPoint other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public static bool operator ==(GridPoint first, GridPoint second)
        {
            return first.X == second.X && first.Y == second.Y && first.Z == second.Z;
        }

        public static bool operator !=(GridPoint first, GridPoint second)
        {
            return first.X != second.X || first.Y != second.Y || first.Z != second.Z;
        }

        public static Vector3 operator +(GridPoint a, GridPoint b)
        {
            return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vector3 operator -(GridPoint a, GridPoint b)
        {
            return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static GridPoint operator -(GridPoint a)
        {
            return new GridPoint(-a.X, -a.Y, -a.Z);
        }

        public static implicit operator int3(GridPoint a)
        {
            return new int3(a.X, a.Y, a.Z);
        }

        public static implicit operator GridPoint(int3 a)
        {
            return new GridPoint(a.x, a.y, a.z);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = X;
                hashCode = (hashCode * 397) ^ Y;
                hashCode = (hashCode * 397) ^ Z;
                return hashCode;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is GridPoint other && Equals(other);
        }

        public override string ToString()
        {
            return $"[{X},{Y},{Z}]";
        }
    }
}