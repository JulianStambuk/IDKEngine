// A couple rarely used SSBOs need to be manually "enabled" by defining macros
// This is so that the limit of 16 SSBOs inside a shader on nvidia isnt hit
// Please https://forums.developer.nvidia.com/t/increase-maximum-allowed-shader-storage-blocks/293755/1

#ifdef DECLARE_MESHLET_STORAGE_BUFFERS
    #define DECLARE_MESHLET_RENDERING_TYPES
#endif

AppInclude(include/GpuTypes.glsl)

layout(std430, binding = 0) restrict buffer DrawElementsCmdSSBO
{
    GpuDrawElementsCmd Commands[];
} drawElementsCmdSSBO;

layout(std430, binding = 1) restrict readonly buffer MeshSSBO
{
    GpuMesh Meshes[];
} meshSSBO;

layout(std430, binding = 2, row_major) restrict readonly buffer MeshInstanceSSBO
{
    GpuMeshInstance MeshInstances[];
} meshInstanceSSBO;

layout(std430, binding = 3) restrict buffer VisibleMeshInstanceSSBO
{
    uint MeshInstanceIDs[];
} visibleMeshInstanceSSBO;

layout(std430, binding = 4) restrict readonly buffer BlasSSBO
{
    GpuBlasNode Nodes[];
} blasSSBO;

#ifdef DECLARE_BVH_TRAVERSAL_STORAGE_BUFFERS // Only used for BVH traversal
layout(std430, binding = 5) restrict readonly buffer BlasTriangleIndicesSSBO
{
    PackedUVec3 Indices[];
} blasTriangleIndicesSSBO;

layout(std430, binding = 6) restrict readonly buffer TlasSSBO
{
    GpuTlasNode Nodes[];
} tlasSSBO;
#endif

layout(std430, binding = 7) restrict buffer WavefrontRaySSBO
{
    GpuWavefrontRay Rays[];
} wavefrontRaySSBO;

layout(std430, binding = 8) restrict buffer WavefrontPTSSBO
{
    GpuDispatchCommand DispatchCommand;
    uint Counts[2];
    uint PingPongIndex;
    uint AccumulatedSamples;
    uint AliveRayIndices[];
} wavefrontPTSSBO;

layout(std430, binding = 9) restrict readonly buffer MaterialSSBO
{
    GpuMaterial Materials[];
} materialSSBO;

layout(std430, binding = 10) restrict buffer VertexSSBO
{
    GpuVertex Vertices[];
} vertexSSBO;

layout(std430, binding = 11) restrict buffer VertexPositionsSSBO
{
    PackedVec3 Positions[];
} vertexPositionsSSBO;

#ifdef DECLARE_MESHLET_STORAGE_BUFFERS // Only used when mesh shader path is taken
layout(std430, binding = 12) restrict buffer MeshletTaskCmdSSBO
{
    GpuMeshletTaskCmd Commands[];
} meshletTaskCmdSSBO;

layout(std430, binding = 13) restrict buffer MeshletTasksCountSSBO
{
    uint Count;
} meshletTasksCountSSBO;

layout(std430, binding = 14) restrict readonly buffer MeshletSSBO
{
    GpuMeshlet Meshlets[];
} meshletSSBO;

layout(std430, binding = 15) restrict readonly buffer MeshletInfoSSBO
{
    GpuMeshletInfo MeshletsInfo[];
} meshletInfoSSBO;

layout(std430, binding = 16) restrict readonly buffer MeshletVertexIndicesSSBO
{
    uint VertexIndices[];
} meshletVertexIndicesSSBO;

layout(std430, binding = 17) restrict readonly buffer MeshletLocalIndicesSSBO
{
    uint PackedIndices[];
} meshletLocalIndicesSSBO;
#endif

#ifdef DECLARE_SKINNING_STORAGE_BUFFERS
layout(std430, binding = 18) restrict readonly buffer JointIndicesSSBO
{
    uvec4 Indices[];
} jointIndicesSSBO;

layout(std430, binding = 19) restrict readonly buffer JointWeightsSSBO
{
    vec4 Weights[];
} jointWeightsSSBO;

layout(std430, binding = 20, row_major) restrict readonly buffer JointMatricesSSBO
{
    mat4x3 Matrices[];
} jointMatricesSSBO;

layout(std430, binding = 21) restrict buffer UnskinnedVertexSSBO
{
    UnskinnedVertex Vertices[];
} unskinnedVertexSSBO;

layout(std430, binding = 22) restrict buffer PrevVertexPositionSSBO
{
    PackedVec3 Positions[];
} prevVertexPositionSSBO;

#endif
