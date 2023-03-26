#if defined(SHADER_API_D3D11) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_D3D11_9X) || defined(SHADER_API_DESKTOP) || defined(SHADER_API_MOBILE)

#include "MatrixQuaternion.cginc"

#include "AtomStruct.cginc"

int AllHalfBonds_Length;
StructuredBuffer<HalfBond> AllHalfBonds;

int AllAtoms_Length;
StructuredBuffer<Atom> AllAtoms;

float Scale;

struct ParticleStruct
{
    float3 vertexPositionWorldSpace;
    float3 normalWorldSpace;
    float3 color;
};

ParticleStruct DrawParticle(float3 vertexPosModelSpace, float3 normalModelSpace, uint instanceID)
{
    ParticleStruct result;

    float3 position = AllAtoms[instanceID].position;
    float3 velocity = AllAtoms[instanceID].velocity;
    float4 rotation = AllAtoms[instanceID].rotation;

    float4x4 rotationMatrix = quaternion_to_matrix(rotation);
    result.normalWorldSpace = mul(rotationMatrix, normalModelSpace);

    result.vertexPositionWorldSpace = mul(rotationMatrix, vertexPosModelSpace * Scale) + position;

    float debugValue = 0;//velocityData.w;
    float speed = saturate(length(velocity) / 3.0f);
    float neighbours = saturate(debugValue / 20.0f);
    result.color = float3(speed, (1.5 - neighbours) * (1.1 - speed), 0.1 + neighbours);

    return result;
}

#endif