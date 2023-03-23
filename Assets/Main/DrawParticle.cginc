#if defined(SHADER_API_D3D11) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_D3D11_9X) || defined(SHADER_API_DESKTOP) || defined(SHADER_API_MOBILE)

#include "MatrixQuaternion.cginc"

int AllParticles_Length;
StructuredBuffer<float4> AllParticles_Position;
StructuredBuffer<float4> AllParticles_Velocity;
StructuredBuffer<float4> AllParticles_Rotation;
float Scale;

struct ParticleStruct
{
    float3 vertexPositionWorldSpace;
    float3 color;
};

ParticleStruct DrawParticle(float3 vertexPosModelSpace, uint instanceID)
{
    ParticleStruct result;

    float4 positionData = AllParticles_Position[instanceID];
    float4 velocityData = AllParticles_Velocity[instanceID];
    float4 rotation = AllParticles_Rotation[instanceID];

    // float4x4 modelMatrix = quaternion_to_matrix(rotation);
    // vertexPos = mul(modelMatrix, vertexPos * Scale) + positionData.xyz;
    // float3 worldNormal = mul(modelMatrix, v.normal);

    result.vertexPositionWorldSpace = vertexPosModelSpace * Scale + positionData.xyz;

    float debugValue = velocityData.w;
    float speed = saturate(length(velocityData.xyz) / 3.0f);
    float neighbours = saturate(debugValue / 20.0f);
    result.color = float3(speed, (1.5 - neighbours) * (1.1 - speed), 0.1 + neighbours);

    return result;
}

#endif