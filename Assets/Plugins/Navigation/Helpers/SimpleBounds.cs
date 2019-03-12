using System;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Providers.Grid;
using UnityEngine;

namespace Navigation.Scripts.Region
{
    /// <summary>
    /// A basic version of 'Bounds' which can be executed outside of unity's environment.
    /// </summary>
    public struct SimpleBounds : IEquatable<SimpleBounds>
    {
        private Vector3 _center;
        private Vector3 _extents;

        /// <summary>
        ///   <para>Creates a new Bounds.</para>
        /// </summary>
        /// <param name="center">The location of the origin of the Bounds.</param>
        /// <param name="size">The dimensions of the Bounds.</param>
        public SimpleBounds(Vector3 center, Vector3 size)
        {
            _center = center;
            _extents = size * 0.5f;
        }

        public override int GetHashCode()
        {
            return Center.GetHashCode() ^ Extents.GetHashCode() << 2;
        }

        public override bool Equals(object other)
        {
            if (!(other is SimpleBounds))
                return false;
            return Equals((SimpleBounds)other);
        }

        public bool Equals(SimpleBounds other)
        {
            return Center.Equals(other.Center) && Extents.Equals(other.Extents);
        }

        public Vector3[] Points => new[]        
        {
            Center + new Vector3(Extents.x, Extents.y, Extents.z),
            Center + new Vector3(Extents.x, Extents.y, -Extents.z),
            Center + new Vector3(Extents.x, -Extents.y, Extents.z),
            Center + new Vector3(Extents.x, -Extents.y, -Extents.z),
            Center + new Vector3(-Extents.x, Extents.y, Extents.z),
            Center + new Vector3(-Extents.x, Extents.y, -Extents.z),
            Center + new Vector3(-Extents.x, -Extents.y, Extents.z),
            Center + new Vector3(-Extents.x, -Extents.y, -Extents.z),
        };

        /// <summary>
        ///   <para>The center of the bounding box.</para>
        /// </summary>
        public Vector3 Center
        {
            get => _center;
            set => _center = value;
        }

        /// <summary>
        ///   <para>The total size of the box. This is always twice as large as the extents.</para>
        /// </summary>
        public Vector3 Size
        {
            get => _extents * 2f;
            set => _extents = value * 0.5f;
        }

        /// <summary>
        ///   <para>The extents of the Bounding Box. This is always half of the size of the Bounds.</para>
        /// </summary>
        public Vector3 Extents
        {
            get => _extents;
            set => _extents = value;
        }

        /// <summary>
        ///   <para>The minimal point of the box. This is always equal to center-extents.</para>
        /// </summary>
        public Vector3 Min
        {
            get => Center - Extents;
            set => SetMinMax(value, Max);
        }

        /// <summary>
        ///   <para>The maximal point of the box. This is always equal to center+extents.</para>
        /// </summary>
        public Vector3 Max
        {
            get => Center + Extents;
            set => SetMinMax(Min, value);
        }

        public static bool operator ==(SimpleBounds lhs, SimpleBounds rhs) => lhs.Center == rhs.Center && lhs.Extents == rhs.Extents;

        public static bool operator !=(SimpleBounds lhs, SimpleBounds rhs) => !(lhs == rhs);

        public static bool operator !=(Bounds lhs, SimpleBounds rhs) => !(lhs == rhs);

        public static bool operator ==(Bounds lhs, SimpleBounds rhs) => !(lhs != rhs);

        public static bool operator !=(SimpleBounds lhs, Bounds rhs) => !(lhs == rhs);

        public static bool operator ==(SimpleBounds lhs, Bounds rhs) => !(lhs != rhs);

        /// <summary>
        ///   <para>Sets the bounds to the min and max value of the box.</para>
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public void SetMinMax(Vector3 min, Vector3 max)
        {
            Extents = (max - min) * 0.5f;
            Center = min + Extents;
        }

        /// <summary>
        ///   <para>Grows the Bounds to include the point.</para>
        /// </summary>
        /// <param name="point"></param>
        public void Encapsulate(Vector3 point)
        {
            SetMinMax(Vector3.Min(Min, point), Vector3.Max(Max, point));
        }

        /// <summary>
        ///   <para>Grow the bounds to encapsulate the bounds.</para>
        /// </summary>
        /// <param name="bounds"></param>
        public void Encapsulate(SimpleBounds bounds)
        {
            Encapsulate(bounds.Center - bounds.Extents);
            Encapsulate(bounds.Center + bounds.Extents);
        }

        /// <summary>
        ///   <para>Grow the bounds to encapsulate the bounds.</para>
        /// </summary>
        /// <param name="bounds"></param>
        public void Encapsulate(Vector3 center, Vector3 extents)
        {
            Encapsulate(center - extents);
            Encapsulate(center + extents);
        }

        /// <summary>
        ///   <para>Expand the bounds by increasing its size by amount along each side.</para>
        /// </summary>
        /// <param name="amount"></param>
        public void Expand(float amount)
        {
            amount *= 0.5f;
            Extents += new Vector3(amount, amount, amount);
        }

        /// <summary>
        ///   <para>Expand the bounds by increasing its size by amount along each side.</para>
        /// </summary>
        /// <param name="amount"></param>
        public void Expand(Vector3 amount)
        {
            Extents += amount * 0.5f;
        }

        /// <summary>
        ///   <para>Does another bounding box intersect with this bounding box?</para>
        /// </summary>
        /// <param name="bounds"></param>
        public bool Intersects(SimpleBounds bounds)
        {
            return Min.x <= (double)bounds.Max.x && Max.x >= (double)bounds.Min.x && 
                   Min.y <= (double)bounds.Max.y && Max.y >= (double)bounds.Min.y && 
                   Min.z <= (double)bounds.Max.z && Max.z >= (double)bounds.Min.z;
        }

        public bool Intersects(Bounds bounds)
        {
            return Min.x <= (double)bounds.max.x && Max.x >= (double)bounds.min.x &&
                   Min.y <= (double)bounds.max.y && Max.y >= (double)bounds.min.y && 
                   Min.z <= (double)bounds.max.z && Max.z >= (double)bounds.min.z;
        }

        //public bool Intersects(Bounds other, SimpleTransform transform)
        //{
        //    var worldBounds = new SimpleBounds(transform.GetWorldPosition(Center), transform.GetWorldPosition(Size));

        //    return worldBounds.Min.x <= (double)other.max.x && worldBounds.Max.x >= (double)other.min.x &&
        //           worldBounds.Min.y <= (double)other.max.y && worldBounds.Max.y >= (double)other.min.y &&
        //           worldBounds.Min.z <= (double)other.max.z && worldBounds.Max.z >= (double)other.min.z;
        //}

        /// <summary>
        ///   <para>Returns a nicely formatted string for the bounds.</para>
        /// </summary>
        /// <param name="format"></param>
        public override string ToString()
        {
            return $"Center: {_center}, Extents: {_extents}";
        }


        ///// <summary>
        /////   <para>Is point contained in the bounding box?</para>
        ///// </summary>
        ///// <param name="point"></param>
        //[NativeMethod("IsInside", IsThreadSafe = true)]
        //public bool Contains(Vector3 point)
        //{
        //    return SimpleBounds.Contains_Injected(ref this, ref point);
        //}

        ///// <summary>
        /////   <para>The smallest squared distance between the point and this bounding box.</para>
        ///// </summary>
        ///// <param name="point"></param>
        //[FreeFunction("BoundsScripting::SqrDistance", HasExplicitThis = true, IsThreadSafe = true)]
        //public float SqrDistance(Vector3 point)
        //{
        //    return SimpleBounds.SqrDistance_Injected(ref this, ref point);
        //}

        //[FreeFunction("IntersectRayAABB", IsThreadSafe = true)]
        //private static bool IntersectRayAabb(Ray ray, SimpleBounds bounds, out float dist)
        //{
        //    return SimpleBounds.IntersectRayAABB_Injected(ref ray, ref bounds, out dist);
        //}

        ///// <summary>
        /////   <para>The closest point on the bounding box.</para>
        ///// </summary>
        ///// <param name="point">Arbitrary point.</param>
        ///// <returns>
        /////   <para>The point on the bounding box or inside the bounding box.</para>
        ///// </returns>
        //[FreeFunction("BoundsScripting::ClosestPoint", HasExplicitThis = true, IsThreadSafe = true)]
        //public Vector3 ClosestPoint(Vector3 point)
        //{
        //    Vector3 ret;
        //    SimpleBounds.ClosestPoint_Injected(ref this, ref point, out ret);
        //    return ret;
        //}

        //[MethodImpl(MethodImplOptions.InternalCall)]
        //private static extern bool Contains_Injected(ref SimpleBounds unitySelf, ref Vector3 point);

        //[MethodImpl(MethodImplOptions.InternalCall)]
        //private static extern float SqrDistance_Injected(ref SimpleBounds unitySelf, ref Vector3 point);

        //[MethodImpl(MethodImplOptions.InternalCall)]
        //private static extern bool IntersectRayAABB_Injected(
        //  ref Ray ray,
        //  ref SimpleBounds bounds,
        //  out float dist);

        //[MethodImpl(MethodImplOptions.InternalCall)]
        //private static extern void ClosestPoint_Injected(
        //  ref SimpleBounds unitySelf,
        //  ref Vector3 point,
        //  out Vector3 ret);
        public bool Contains(Vector3 vecPoint)
        {
            return vecPoint.x > Min.x && vecPoint.x < Max.x &&
                   vecPoint.y > Min.y && vecPoint.y < Max.y &&
                   vecPoint.z > Min.z && vecPoint.z < Max.z;
        }
    }
}
