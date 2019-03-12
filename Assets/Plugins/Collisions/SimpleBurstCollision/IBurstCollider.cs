
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vella.SimpleBurstCollision
{
    public interface IBurstCollider
    {

    }

    public static class IBurstColliderExtensions
    {
        public static IEnumerable<T> OfType<T>(this IEnumerable<BurstBaseCollider> collection) where T : IBurstCollider
        {
            foreach (var item in collection)
            {
                switch (item.Type)
                {
                    case BurstColliderType.Box:
                        if (typeof(T) == typeof(BurstBoxCollider))
                        {
                            yield return (T)Convert.ChangeType(item.Box, typeof(T));
                        }
                        break;
                    case BurstColliderType.Sphere:
                        if (typeof(T) == typeof(SphereCollider))
                        {
                            yield return (T)Convert.ChangeType(item.Box, typeof(T));
                        }
                        break;
                }
            }
        }
    }
}