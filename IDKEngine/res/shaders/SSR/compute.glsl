#version 460 core
#define EPSILON 0.001
#extension GL_ARB_bindless_texture : require

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(binding = 0) restrict writeonly uniform image2D ImgResult;
layout(binding = 0) uniform sampler2D SamplerSrc;

layout(std140, binding = 0) uniform BasicDataUBO
{
    mat4 ProjView;
    mat4 View;
    mat4 InvView;
    vec3 ViewPos;
    float _pad0;
    mat4 Projection;
    mat4 InvProjection;
    mat4 InvProjView;
    mat4 PrevProjView;
    float NearPlane;
    float FarPlane;
    float DeltaUpdate;
    float Time;
} basicDataUBO;

layout(std140, binding = 4) uniform SkyBoxUBO
{
    samplerCube Albedo;
} skyBoxUBO;

layout(std140, binding = 6) uniform GBufferDataUBO
{
    sampler2D AlbedoAlpha;
    sampler2D NormalSpecular;
    sampler2D EmissiveRoughness;
    sampler2D Velocity;
    sampler2D Depth;
} gBufferDataUBO;

vec3 SSR(vec3 normal, vec3 fragPos);
void CustomBinarySearch(vec3 samplePoint, vec3 deltaStep, inout vec3 projectedSample);
vec3 ViewToNDC(vec3 viewPos);
vec3 NDCToView(vec3 ndc);

uniform int Samples;
uniform int BinarySearchSamples;
uniform float MaxDist;

void main()
{
    ivec2 imgCoord = ivec2(gl_GlobalInvocationID.xy);
    vec2 uv = (imgCoord + 0.5) / imageSize(ImgResult);

    vec4 normalSpec = texture(gBufferDataUBO.NormalSpecular, uv);
    float depth = texture(gBufferDataUBO.Depth, uv).r;
    if (normalSpec.a < EPSILON || depth == 1.0)
    {
        imageStore(ImgResult, imgCoord, vec4(0.0));
        return;
    }

    vec3 fragPos = NDCToView(vec3(uv, depth) * 2.0 - 1.0);
    vec3 color = SSR(normalize((basicDataUBO.View * vec4(normalSpec.rgb, 0.0)).xyz), fragPos);

    imageStore(ImgResult, imgCoord, vec4(color * normalSpec.a, 1.0));
}

vec3 SSR(vec3 normal, vec3 fragPos)
{
    // Viewpos is origin in view space 
    const vec3 VIEW_POS = vec3(0.0);
    vec3 reflectDir = reflect(normalize(fragPos - VIEW_POS), normal);
    vec3 maxReflectPoint = fragPos + reflectDir * MaxDist;
    vec3 deltaStep = (maxReflectPoint - fragPos) / Samples;

    vec3 samplePoint = fragPos;
    for (int i = 0; i < Samples; i++)
    {
        samplePoint += deltaStep;

        vec3 projectedSample = ViewToNDC(samplePoint) * 0.5 + 0.5;
        if (any(greaterThanEqual(projectedSample.xy, vec2(1.0))) || any(lessThan(projectedSample.xy, vec2(0.0))) || projectedSample.z > 1.0)
        {
            // TODO: Parallax corrected cubemap reflections as fallback? 
            return vec3(0.0);
        }

        float depth = texture(gBufferDataUBO.Depth, projectedSample.xy).r;
        if (projectedSample.z > depth)
        {
            CustomBinarySearch(samplePoint, deltaStep, projectedSample);
            return texture(SamplerSrc, projectedSample.xy).rgb; 
        }

    }

    vec3 worldSpaceReflectDir = (basicDataUBO.InvView * vec4(reflectDir, 0.0)).xyz;
    return texture(skyBoxUBO.Albedo, worldSpaceReflectDir).rgb;
}

void CustomBinarySearch(vec3 samplePoint, vec3 deltaStep, inout vec3 projectedSample)
{
    // Go back one step at the beginning because we know we are to far
    deltaStep *= 0.5;
    samplePoint -= deltaStep * 0.5;
    for (int i = 1; i < BinarySearchSamples; i++)
    {
        projectedSample = ViewToNDC(samplePoint) * 0.5 + 0.5;
        float depth = texture(gBufferDataUBO.Depth, projectedSample.xy).r;

        deltaStep *= 0.5;
        if (projectedSample.z > depth)
        {
            samplePoint -= deltaStep;
        }
        else
        {
            samplePoint += deltaStep;
        }
    }
}

vec3 ViewToNDC(vec3 viewPos)
{
    vec4 clipPos = basicDataUBO.Projection * vec4(viewPos, 1.0);
    return clipPos.xyz / clipPos.w;
}

vec3 NDCToView(vec3 ndc)
{
    vec4 viewPos = basicDataUBO.InvProjection * vec4(ndc, 1.0);
    return viewPos.xyz / viewPos.w;
}