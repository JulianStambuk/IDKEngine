﻿using System;
using System.Linq;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using IDKEngine.Render.Objects;
using IDKEngine.Shapes;

namespace IDKEngine
{
    class BVH : IDisposable
    {
        public const bool CPU_USE_TLAS = false;

        public struct RayHitInfo
        {
            public GpuBlasTriangle Triangle;
            public Vector3 Bary;
            public float T;
            public int MeshID;
            public int InstanceID;
        }

        public struct PrimitiveHitInfo
        {
            public GpuBlasTriangle Triangle;
            public int MeshID;
            public int InstanceID;
        }

        public readonly TLAS Tlas;

        private readonly BufferObject blasBuffer;
        private readonly BufferObject blasTriangleBuffer;
        private readonly BufferObject tlasBuffer;
        public BVH()
        {
            blasBuffer = new BufferObject();
            blasBuffer.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 5);

            blasTriangleBuffer = new BufferObject();
            blasTriangleBuffer.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 6);

            tlasBuffer = new BufferObject();
            tlasBuffer.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 7);

            Tlas = new TLAS();
        }

        public bool Intersect(in Ray ray, out RayHitInfo hitInfo, float tMax = float.MaxValue)
        {
            if (CPU_USE_TLAS)
            {
                return Tlas.Intersect(ray, out hitInfo, tMax);
            }
            else
            {
                hitInfo = new RayHitInfo();
                hitInfo.T = tMax;

                for (int i = 0; i < Tlas.Blases.Count; i++)
                {
                    BLAS blas = Tlas.Blases[i];
                    ref readonly GpuDrawElementsCmd drawCmd = ref Tlas.DrawCommands[i];

                    for (int j = 0; j < drawCmd.InstanceCount; j++)
                    {
                        int instanceID = drawCmd.BaseInstance + j;
                        ref readonly GpuMeshInstance meshInstance = ref Tlas.MeshInstances[instanceID];

                        Ray localRay = ray.Transformed(meshInstance.InvModelMatrix);
                        if (blas.Intersect(localRay, out BLAS.RayHitInfo blasHitInfo, hitInfo.T))
                        {
                            hitInfo.Triangle = blasHitInfo.Triangle;
                            hitInfo.Bary = blasHitInfo.Bary;
                            hitInfo.T = blasHitInfo.T;

                            hitInfo.MeshID = i;
                            hitInfo.InstanceID = instanceID;
                        }
                    }

                }

                return hitInfo.T != tMax;
            }
        }

        public delegate void IntersectFunc(in PrimitiveHitInfo hitInfo);
        public void Intersect(in Box box, IntersectFunc intersectFunc)
        {
            for (int i = 0; i < Tlas.Blases.Count; i++)
            {
                BLAS blas = Tlas.Blases[i];
                ref readonly GpuDrawElementsCmd drawCmd = ref Tlas.DrawCommands[i];

                for (int j = 0; j < drawCmd.InstanceCount; j++)
                {
                    int instanceID = drawCmd.BaseInstance + j;
                    ref readonly GpuMeshInstance meshInstance = ref Tlas.MeshInstances[instanceID];

                    Box localBox = Box.Transformed(box, meshInstance.InvModelMatrix);
                    blas.Intersect(localBox, (in GpuBlasTriangle triangle) =>
                    {
                        PrimitiveHitInfo hitInfo;
                        hitInfo.Triangle = triangle;
                        hitInfo.MeshID = i;
                        hitInfo.InstanceID = instanceID;

                        intersectFunc(hitInfo);
                    });
                }
            }
        }

        /// <summary>
        /// Builds new BLAS'es from the associated mesh data, updates the TLAS for them and updates the corresponding GPU buffers.
        /// Building BLAS'es is expected to be slow. The process is done in parallel so prefer to pass multiple meshes at once to better utilize the hardware.
        /// </summary>
        /// <param name="meshInstances"></param>
        /// <param name="newMeshesDrawCommands"></param>
        /// <param name="vertexPositions"></param>
        /// <param name="indices"></param>
        public void AddMeshesAndBuild(ReadOnlyMemory<GpuDrawElementsCmd> newMeshesDrawCommands, GpuDrawElementsCmd[] drawCommands, GpuMeshInstance[] meshInstances, Vector3[] vertexPositions, uint[] indices)
        {
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            BLAS[] newBlasInstances = CreateBlasesFromGeometry(newMeshesDrawCommands, vertexPositions, indices);

            AddBlases(newBlasInstances, drawCommands, meshInstances);
            Logger.Log(Logger.LogLevel.Info, $"Created {newBlasInstances.Length} new Bottom Level Acceleration Structures (BLAS) in {sw.ElapsedMilliseconds} milliseconds");

            sw.Restart();
            TlasBuild();
            Logger.Log(Logger.LogLevel.Info, $"Created Top Level Acceleration Structures (TLAS) for {Tlas.MeshInstances.Length} instances in {sw.ElapsedMilliseconds} milliseconds");
        }

        private void AddBlases(BLAS[] blases, GpuDrawElementsCmd[] drawCommands, GpuMeshInstance[] meshInstances)
        {
            Tlas.AddBlases(blases, drawCommands, meshInstances);
            SetBlasBuffersContent();
        }

        public void TlasBuild()
        {
            Tlas.Build();
            SetTlasBufferContent(Tlas.Nodes);
        }

        public int GetBlasesTriangleCount()
        {
            return Tlas.Blases.Sum(blasInstances => blasInstances.Triangles.Length);
        }


        private unsafe void SetBlasBuffersContent()
        {
            blasBuffer.MutableAllocate((nint)sizeof(GpuBlasNode) * Tlas.Blases.Sum(blas => blas.Nodes.Length), IntPtr.Zero);
            blasTriangleBuffer.MutableAllocate((nint)sizeof(GpuBlasTriangle) * GetBlasesTriangleCount(), IntPtr.Zero);

            nint uploadedBlasNodesCount = 0;
            nint uploadedTrianglesCount = 0;
            for (int i = 0; i < Tlas.Blases.Count; i++)
            {
                BLAS blas = Tlas.Blases[i];

                blasBuffer.SubData(uploadedBlasNodesCount * sizeof(GpuBlasNode), blas.Nodes.Length * (nint)sizeof(GpuBlasNode), blas.Nodes);
                blasTriangleBuffer.SubData(uploadedTrianglesCount * sizeof(GpuBlasTriangle), blas.Triangles.Length * (nint)sizeof(GpuBlasTriangle), blas.Triangles);

                uploadedBlasNodesCount += blas.Nodes.Length;
                uploadedTrianglesCount += blas.Triangles.Length;
            }
        }
        private unsafe void SetTlasBufferContent(ReadOnlySpan<GpuTlasNode> tlasNodes)
        {
            tlasBuffer.MutableAllocate(sizeof(GpuTlasNode) * tlasNodes.Length, tlasNodes[0]);
        }


        public void Dispose()
        {
            blasTriangleBuffer.Dispose();
            blasBuffer.Dispose();
        }

        private static BLAS[] CreateBlasesFromGeometry(ReadOnlyMemory<GpuDrawElementsCmd> drawCommands, Vector3[] vertexPositions, uint[] vertexIndices)
        {
            BLAS[] blases = new BLAS[drawCommands.Length];

            Parallel.For(0, blases.Length, i =>
            //for (int i = 0; i < blases.Length; i++)
            {
                ref readonly GpuDrawElementsCmd cmd = ref drawCommands.Span[i];

                GpuBlasTriangle[] blasTriangles = new GpuBlasTriangle[cmd.Count / 3];
                for (int j = 0; j < blasTriangles.Length; j++)
                {
                    {
                        uint index = vertexIndices[cmd.FirstIndex + (j * 3) + 0];
                        blasTriangles[j].Position0 = vertexPositions[(uint)cmd.BaseVertex + index];
                        blasTriangles[j].VertexIndex0 = index;
                    }
                    {
                        uint index = vertexIndices[cmd.FirstIndex + (j * 3) + 1];
                        blasTriangles[j].Position1 = vertexPositions[(uint)cmd.BaseVertex + index];
                        blasTriangles[j].VertexIndex1 = index;
                    }
                    {
                        uint index = vertexIndices[cmd.FirstIndex + (j * 3) + 2];
                        blasTriangles[j].Position2 = vertexPositions[(uint)cmd.BaseVertex + index];
                        blasTriangles[j].VertexIndex2 = index;
                    }
                }

                BLAS blas = new BLAS(blasTriangles);
                blas.Build();

                blases[i] = blas;
            });

            return blases;
        }
    }
}
