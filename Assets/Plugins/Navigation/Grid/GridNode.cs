using System;
using System.Drawing;
using Navigation.Scripts.Region;
using Vella.Common.Collections;
using Vella.Common.Navigation;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Analytics;

namespace Providers.Grid
{
    public struct GridNode : IEquatable<GridNode>
    {
        public float X;
        public float Y;
        public float Z;
        public Vector3 Center;
        public Vector3 NavigableCenter;
        public Vector3 Normal;
        public GridPoint GridPoint;
        public SimpleBounds Bounds;
        public Vector3 Size;
        public ulong Flags;

        public float GScore;
        public float HScore;
        public float FScore;
        public GridPoint ParentPoint;
        public int OpenId;
        public int ClosedId;

        public static bool operator ==(GridNode first, GridNode second)
        {
            return first.Equals(second);
        }

        public static bool operator !=(GridNode first, GridNode second)
        {
            return !(first.Equals(second));
        }

        public bool IsWalkable
        {
            get => ((NodeFlags)Flags & NodeFlags.AllowWalk) != 0;
            set => SetFlag(NodeFlags.AllowWalk, value);
        }
        
        public bool Equals(GridNode other)
        {
            return GridPoint == other.GridPoint;
        }

        public override int GetHashCode()
        {
            return (int)Center.x * 13 ^ (int)Center.y * 23;
        }

        private void SetFlag(NodeFlags flag, bool value)
        {
            if (value)
            {
                AddFlags(flag);
            }
            else
            {
                RemoveFlags(flag);
            }
        }

        public bool AddFlags(NodeFlags flags)
        {
            if (flags != NodeFlags.None)
            {
                Flags |= (ulong)flags;
                return true;
            }                
            return false;
        }
        public bool HasFlag(NodeFlags flags)
        {
            return (Flags & (ulong)flags) != 0; 
        }

        public bool RemoveFlags(NodeFlags flags)
        {
            if (flags != NodeFlags.None)
            {             
                Flags &= ~(ulong)flags;
                return true;
            }                
            return false;
        }

        public bool RemoveArea(NodeFlags flags)
        {
            if (flags != NodeFlags.None)
            {                
                Flags &= ~(ulong)flags;
                return true;
            }
            return false;
        }

        public void ResetNodeFlags()
        {
            Flags = 0;
        }

        internal void Reset()
        {
            ResetNodeFlags();
        }

    }
}
