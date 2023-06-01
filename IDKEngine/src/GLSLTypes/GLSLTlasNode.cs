﻿using OpenTK.Mathematics;

namespace IDKEngine
{
    struct GLSLTlasNode
    {
        public Vector3 Min;
        public uint LeftChild;

        public Vector3 Max;
        public uint BlasIndex;

        public bool IsLeaf()
        {
            return LeftChild == 0;
        }
    }
}