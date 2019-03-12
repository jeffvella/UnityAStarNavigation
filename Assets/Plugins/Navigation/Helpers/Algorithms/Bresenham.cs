using System.Collections.Generic;
using Providers.Grid;
using UnityEngine;

namespace Grid.Algorithms
{
    public class Bresenham
    {
        public static IEnumerable<Vector3> GetPointsOnLine(Vector3 pStart, Vector3 pEnd, float pSteps = 1)
        {
            return GetPointsOnLine((int)pStart.x, (int)pStart.y, (int)pStart.z, (int)pEnd.x, (int)pEnd.y, (int)pEnd.z);
        }

        public static IEnumerable<Vector3> GetPointsOnLine(int aX, int aY, int aZ, int bX, int bY, int bZ, float pSteps = 1)
        {
            Vector3 result;

            int xd, yd, zd;
            int ax, ay, az;
            int sx, sy, sz;
            int dx, dy, dz;

            dx = bX - aX;
            dy = bY - aY;
            dz = bZ - aZ;

            ax = Mathf.Abs(dx) << 1;
            ay = Mathf.Abs(dy) << 1;
            az = Mathf.Abs(dz) << 1;

            sx = (int)Mathf.Sign(dx);
            sy = (int)Mathf.Sign(dy);
            sz = (int)Mathf.Sign(dz);


            if (ax >= Mathf.Max(ay, az)) // x dominant
            {
                yd = ay - (ax >> 1);
                zd = az - (ax >> 1);
                for (; ; )
                {
                    result.x = (int)(aX / pSteps);
                    result.y = (int)(aY / pSteps);
                    result.z = (int)(aZ / pSteps);
                    yield return result;

                    if (aX == bX)
                        yield break;

                    if (yd >= 0)
                    {
                        aY += sy;
                        yd -= ax;
                    }

                    if (zd >= 0)
                    {
                        aZ += sz;
                        zd -= ax;
                    }

                    aX += sx;
                    yd += ay;
                    zd += az;
                }
            }

            if (ay >= Mathf.Max(ax, az)) // y dominant
            {
                xd = ax - (ay >> 1);
                zd = az - (ay >> 1);
                for (; ; )
                {
                    result.x = (int)(aX / pSteps);
                    result.y = (int)(aY / pSteps);
                    result.z = (int)(aZ / pSteps);
                    yield return result;

                    if (aY == bY)
                        yield break;

                    if (xd >= 0)
                    {
                        aX += sx;
                        xd -= ay;
                    }

                    if (zd >= 0)
                    {
                        aZ += sz;
                        zd -= ay;
                    }

                    aY += sy;
                    xd += ax;
                    zd += az;
                }
            }

            if (az >= Mathf.Max(ax, ay)) // z dominant
            {
                xd = ax - (az >> 1);
                yd = ay - (az >> 1);
                for (; ; )
                {
                    result.x = (int)(aX / pSteps);
                    result.y = (int)(aY / pSteps);
                    result.z = (int)(aZ / pSteps);
                    yield return result;

                    if (aZ == bZ)
                        yield break;

                    if (xd >= 0)
                    {
                        aX += sx;
                        xd -= az;
                    }

                    if (yd >= 0)
                    {
                        aY += sy;
                        yd -= az;
                    }

                    aZ += sz;
                    xd += ax;
                    yd += ay;
                }
            }
        }
    }
}
