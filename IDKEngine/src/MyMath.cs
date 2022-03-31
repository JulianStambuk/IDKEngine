﻿using System;
using OpenTK.Mathematics;

namespace IDKEngine
{
    static class MyMath
    {
        // Source: https://github.com/leesg213/TemporalAA/blob/main/Renderer/AAPLRenderer.mm#L152
        public static void GetHaltonSequence_2_3(Span<float> buffer)
        {
            int n2 = 0, d2 = 1, n3 = 0, d3 = 1;
            for (int i = 0; i < buffer.Length; i += 2)
            {
                buffer[i + 0] = GetHalton(2, ref n2, ref d2);
                buffer[i + 1] = GetHalton(3, ref n3, ref d3);
            }
        }

        // Source: https://github.com/leesg213/TemporalAA/blob/main/Renderer/AAPLRenderer.mm#L124
        public static float GetHalton(int baseHalton, ref int n, ref int d)
        {
            int x = d - n;
            if (x == 1)
            {
                n = 1;
                d *= baseHalton;
            }
            else
            {
                int y = d / baseHalton;
                while(x <= y)
                {
                    y /= baseHalton;
                }

                n = (baseHalton + 1) * y - x;
            }

            float result = n / (float)d;
            return result;
        }
        
        public static void MapHaltonSequence(Span<float> halton, float width, float height)
        {
            for (int i = 0; i < halton.Length; i += 2)
            {
                halton[i + 0] = (halton[i + 0] * 2.0f - 1.0f) / width;
                halton[i + 1] = (halton[i + 1] * 2.0f - 1.0f) / height;
            }
        }

        public static void BitsInsert(ref uint mem, uint data, int offset, int bits)
        {
            mem |= GetBits(data, 0, bits) << offset;
        }

        public static uint GetBits(uint data, int offset, int bits)
        {
            return data & (((1u << bits) - 1u) << offset);
        }

        public static bool TriangleVSBox(Vector3 a, Vector3 b, Vector3 c, Vector3 boxCenter, Vector3 boxExtents)
        {
            // From the book "Real-Time Collision Detection" by Christer Ericson, page 169
            // See also the published Errata at http://realtimecollisiondetection.net/books/rtcd/errata/

            // Translate triangle as conceptually moving AABB to origin
            var v0 = (a - boxCenter);
            var v1 = (b - boxCenter);
            var v2 = (c - boxCenter);

            // Compute edge vectors for triangle
            var f0 = (v1 - v0);
            var f1 = (v2 - v1);
            var f2 = (v0 - v2);

            #region Test axes a00..a22 (category 3)

            // Test axis a00
            var a00 = new Vector3(0, -f0.Z, f0.Y);
            var p0 = Vector3.Dot(v0, a00);
            var p1 = Vector3.Dot(v1, a00);
            var p2 = Vector3.Dot(v2, a00);
            var r = boxExtents.Y * Math.Abs(f0.Z) + boxExtents.Z * Math.Abs(f0.Y);
            if (Math.Max(-Fmax(p0, p1, p2), Fmin(p0, p1, p2)) > r)
            {
                return false;
            }

            // Test axis a01
            var a01 = new Vector3(0, -f1.Z, f1.Y);
            p0 = Vector3.Dot(v0, a01);
            p1 = Vector3.Dot(v1, a01);
            p2 = Vector3.Dot(v2, a01);
            r = boxExtents.Y * Math.Abs(f1.Z) + boxExtents.Z * Math.Abs(f1.Y);
            if (Math.Max(-Fmax(p0, p1, p2), Fmin(p0, p1, p2)) > r)
            {
                return false;
            }

            // Test axis a02
            var a02 = new Vector3(0, -f2.Z, f2.Y);
            p0 = Vector3.Dot(v0, a02);
            p1 = Vector3.Dot(v1, a02);
            p2 = Vector3.Dot(v2, a02);
            r = boxExtents.Y * Math.Abs(f2.Z) + boxExtents.Z * Math.Abs(f2.Y);
            if (Math.Max(-Fmax(p0, p1, p2), Fmin(p0, p1, p2)) > r)
            {
                return false;
            }

            // Test axis a10
            var a10 = new Vector3(f0.Z, 0, -f0.X);
            p0 = Vector3.Dot(v0, a10);
            p1 = Vector3.Dot(v1, a10);
            p2 = Vector3.Dot(v2, a10);
            r = boxExtents.X * Math.Abs(f0.Z) + boxExtents.Z * Math.Abs(f0.X);
            if (Math.Max(-Fmax(p0, p1, p2), Fmin(p0, p1, p2)) > r)
            {
                return false;
            }

            // Test axis a11
            var a11 = new Vector3(f1.Z, 0, -f1.X);
            p0 = Vector3.Dot(v0, a11);
            p1 = Vector3.Dot(v1, a11);
            p2 = Vector3.Dot(v2, a11);
            r = boxExtents.X * Math.Abs(f1.Z) + boxExtents.Z * Math.Abs(f1.X);
            if (Math.Max(-Fmax(p0, p1, p2), Fmin(p0, p1, p2)) > r)
            {
                return false;
            }

            // Test axis a12
            var a12 = new Vector3(f2.Z, 0, -f2.X);
            p0 = Vector3.Dot(v0, a12);
            p1 = Vector3.Dot(v1, a12);
            p2 = Vector3.Dot(v2, a12);
            r = boxExtents.X * Math.Abs(f2.Z) + boxExtents.Z * Math.Abs(f2.X);
            if (Math.Max(-Fmax(p0, p1, p2), Fmin(p0, p1, p2)) > r)
            {
                return false;
            }

            // Test axis a20
            var a20 = new Vector3(-f0.Y, f0.X, 0);
            p0 = Vector3.Dot(v0, a20);
            p1 = Vector3.Dot(v1, a20);
            p2 = Vector3.Dot(v2, a20);
            r = boxExtents.X * Math.Abs(f0.Y) + boxExtents.Y * Math.Abs(f0.X);
            if (Math.Max(-Fmax(p0, p1, p2), Fmin(p0, p1, p2)) > r)
            {
                return false;
            }

            // Test axis a21
            var a21 = new Vector3(-f1.Y, f1.X, 0);
            p0 = Vector3.Dot(v0, a21);
            p1 = Vector3.Dot(v1, a21);
            p2 = Vector3.Dot(v2, a21);
            r = boxExtents.X * Math.Abs(f1.Y) + boxExtents.Y * Math.Abs(f1.X);
            if (Math.Max(-Fmax(p0, p1, p2), Fmin(p0, p1, p2)) > r)
            {
                return false;
            }

            // Test axis a22
            var a22 = new Vector3(-f2.Y, f2.X, 0);
            p0 = Vector3.Dot(v0, a22);
            p1 = Vector3.Dot(v1, a22);
            p2 = Vector3.Dot(v2, a22);
            r = boxExtents.X * Math.Abs(f2.Y) + boxExtents.Y * Math.Abs(f2.X);
            if (Math.Max(-Fmax(p0, p1, p2), Fmin(p0, p1, p2)) > r)
            {
                return false;
            }

            #endregion

            #region Test the three axes corresponding to the face normals of AABB b (category 1)

            // Exit if...
            // ... [-extents.x, extents.x] and [min(v0.x,v1.x,v2.x), max(v0.x,v1.x,v2.x)] do not overlap
            if (Fmax(v0.X, v1.X, v2.X) < -boxExtents.X || Fmin(v0.X, v1.X, v2.X) > boxExtents.X)
            {
                return false;
            }

            // ... [-extents.y, extents.y] and [min(v0.y,v1.y,v2.y), max(v0.y,v1.y,v2.y)] do not overlap
            if (Fmax(v0.Y, v1.Y, v2.Y) < -boxExtents.Y || Fmin(v0.Y, v1.Y, v2.Y) > boxExtents.Y)
            {
                return false;
            }

            // ... [-extents.z, extents.z] and [min(v0.z,v1.z,v2.z), max(v0.z,v1.z,v2.z)] do not overlap
            if (Fmax(v0.Z, v1.Z, v2.Z) < -boxExtents.Z || Fmin(v0.Z, v1.Z, v2.Z) > boxExtents.Z)
            {
                return false;
            }

            #endregion

            #region Test separating axis corresponding to triangle face normal (category 2)

            var planeNormal = Vector3.Cross(f0, f1);
            var planeDistance = Vector3.Dot(planeNormal, v0);

            // Compute the projection interval radius of b onto L(t) = b.c + t * p.n
            r = boxExtents.X * Math.Abs(planeNormal.X) + boxExtents.Y * Math.Abs(planeNormal.Y) + boxExtents.Z * Math.Abs(planeNormal.Z);

            // Intersection occurs when plane distance falls within [-r,+r] interval
            if (planeDistance > r)
            {
                return false;
            }

            #endregion

            return true;

            static float Fmin(float a, float b, float c)
            {
                return MathF.Min(a, MathF.Min(b, c));
            }
            static float Fmax(float a, float b, float c)
            {
                return MathF.Max(a, MathF.Max(b, c));
            }
        }
    }
}
