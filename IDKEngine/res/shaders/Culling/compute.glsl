#version 460 core

layout(local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

struct Frustum
{
    vec4 Planes[6];
};

struct DrawCommand
{
    int Count;
    int InstanceCount;
    int FirstIndex;
    int BaseVertex;
    int BaseInstance;
};

struct Node
{
    vec3 Min;
    uint TriStartOrLeftChild;
    vec3 Max;
    uint TriCount;
};

struct Mesh
{
    int InstanceCount;
    int VisibleCubemapFacesInfo;
    int MaterialIndex;
    float Emissive;
    float NormalMapStrength;
    float SpecularBias;
    float RoughnessBias;
    float RefractionChance;
};

layout(std430, binding = 0) restrict buffer DrawCommandsSSBO
{
    DrawCommand DrawCommands[];
} drawCommandsSSBO;

layout(std430, binding = 1) restrict readonly buffer BVHSSBO
{
    Node Nodes[];
} bvhSSBO;

layout(std430, binding = 2) restrict readonly buffer MeshSSBO
{
    Mesh Meshes[];
} meshSSBO;

layout(std430, binding = 4) restrict readonly buffer MatrixSSBO
{
    mat4 Models[];
} matrixSSBO;

vec3 NegativeVertex(Node node, vec3 normal);
bool AABBVsFrustum(Frustum frustum, Node node);
Frustum ExtractFrustum(mat4 projViewModel);

layout(location = 0) uniform mat4 ProjView;

void main()
{
    uint meshIndex = gl_GlobalInvocationID.x;
    if (meshIndex >= meshSSBO.Meshes.length())
        return;

    DrawCommand meshCMD = drawCommandsSSBO.DrawCommands[meshIndex];
    Node node = bvhSSBO.Nodes[meshCMD.FirstIndex / 3];
    
    const int glInstanceID = 0; // TODO: Derive from built in variables
    mat4 model = matrixSSBO.Models[meshCMD.BaseInstance + glInstanceID];
    
    Frustum frustum = ExtractFrustum(ProjView * model);
    drawCommandsSSBO.DrawCommands[meshIndex].InstanceCount = int(AABBVsFrustum(frustum, node));
}

Frustum ExtractFrustum(mat4 projViewModel)
{
    Frustum frustum;
	for (int i = 0; i < 3; ++i)
    {
        for (int j = 0; j < 2; ++j)
        {
            frustum.Planes[i * 2 + j].x = projViewModel[0][3] + (j == 0 ? projViewModel[0][i] : -projViewModel[0][i]);
            frustum.Planes[i * 2 + j].y = projViewModel[1][3] + (j == 0 ? projViewModel[1][i] : -projViewModel[1][i]);
            frustum.Planes[i * 2 + j].z = projViewModel[2][3] + (j == 0 ? projViewModel[2][i] : -projViewModel[2][i]);
            frustum.Planes[i * 2 + j].w = projViewModel[3][3] + (j == 0 ? projViewModel[3][i] : -projViewModel[3][i]);
            frustum.Planes[i * 2 + j] *= length(frustum.Planes[i * 2 + j].xyz);
        }
    }
	return frustum;
}

bool AABBVsFrustum(Frustum frustum, Node node)
{
	float a = 1.0;

	for (int i = 0; i < 6 && a >= 0.0; ++i) {
		vec3 negative = NegativeVertex(node, frustum.Planes[i].xyz);

		a = dot(vec4(negative, 1.0), frustum.Planes[i]);
	}

	return a >= 0.0;
}

vec3 NegativeVertex(Node node, vec3 normal)
{
	return mix(node.Min, node.Max, greaterThan(normal, vec3(0.0)));
}
