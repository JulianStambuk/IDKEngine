﻿using System.Runtime.Intrinsics;
using System.Runtime.InteropServices;
using OpenTK.Mathematics;
using IDKEngine.Bvh;

namespace IDKEngine.Shapes
{
    [StructLayout(LayoutKind.Explicit)]
    public record struct Box
    {
        public Vector3 this[int index]
        {
            get
            {
                System.Diagnostics.Debug.Assert(index < 8);
                bool isMaxX = (index & 1) != 0;
                bool isMaxY = (index & 2) != 0;
                bool isMaxZ = (index & 4) != 0;
                return new Vector3(isMaxX ? Max.X : Min.X, isMaxY ? Max.Y : Min.Y, isMaxZ ? Max.Z : Min.Z);
            }
        }


        [FieldOffset(0)] public Vector3 Min;
        [FieldOffset(16)] public Vector3 Max;

        [FieldOffset(0)] public Vector128<float> SIMDMin;
        [FieldOffset(16)] public Vector128<float> SIMDMax;

        public Box(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public void GrowToFit(in Vector128<float> point)
        {
            SIMDMin = Vector128.Min(SIMDMin, point);
            SIMDMax = Vector128.Max(SIMDMax, point);
        }

        public void GrowToFit(in Box box)
        {
            SIMDMin = Vector128.Min(SIMDMin, box.SIMDMin);
            SIMDMax = Vector128.Max(SIMDMax, box.SIMDMax);
        }

        public void GrowToFit(Vector3 point)
        {
            Vector128<float> p = Vector128.Create(point.X, point.Y, point.Z, 0.0f);
            GrowToFit(p);
        }

        public void GrowToFit(in BLAS.Triangle tri)
        {
            GrowToFit(tri.Position0);
            GrowToFit(tri.Position1);
            GrowToFit(tri.Position2);
        }

        public Vector3 Center()
        {
            return (Max + Min) * 0.5f;
        }

        public Vector3 Size()
        {
            return Max - Min;
        }

        public Vector3 HalfSize()
        {
            return Size() * 0.5f;
        }

        public float Volume()
        {
            Vector3 size = Size();
            return size.X * size.Y * size.Z;
        }

        public float HalfArea()
        {
            Vector3 size = Size();
            float area = (size.X + size.Y) * size.Z + size.X * size.Y;
            return area;
        }
        

        public void Transform(in Matrix4 matrix)
        {
            this = Transformed(this, matrix);
        }

        public static Box Transformed(in Box box, in Matrix4 matrix)
        {
            // TODO: This function is unreasonable slow in debugger. The indexer and the matrix muls take time
            Box newBox = Empty();
            for (int i = 0; i < 8; i++)
            {
                newBox.GrowToFit((new Vector4(box[i], 1.0f) * matrix).Xyz);
            }
            return newBox;
        }

        public static Vector3 GetOverlappingExtends(in Box a, in Box b)
        {
            Box boundingBox = a;
            boundingBox.GrowToFit(b);

            Vector3 addedSize = a.Size() + b.Size();
            Vector3 extends = addedSize - boundingBox.Size();

            return extends;
        }

        public static Box Empty()
        {
            return new Box(new Vector3(float.MaxValue), new Vector3(float.MinValue));
        }
    }
}
