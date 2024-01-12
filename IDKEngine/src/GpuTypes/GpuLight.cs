﻿using OpenTK.Mathematics;

namespace IDKEngine
{
    public struct GpuLight
    {
        public Vector3 Position;
        public float Radius;
        public Vector3 Color;
        public int PointShadowIndex;
        public Vector3 PrevPosition;
        private readonly float _pad0;
    }
}
