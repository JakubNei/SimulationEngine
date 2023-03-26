#if defined(SHADER_API_D3D11) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_D3D11_9X) || defined(SHADER_API_DESKTOP) || defined(SHADER_API_MOBILE)

#include "MatrixQuaternion.cginc"

#include "AtomStruct.cginc"

int AllHalfBonds_Length;
StructuredBuffer<HalfBond> AllHalfBonds;

int AllAtoms_Length;
StructuredBuffer<Atom> AllAtoms;

float AtomRadius;
float AtomInteractionMaxRadius;

struct DrawAtomResult
{
    float3 vertexPositionWorldSpace;
    float3 normalWorldSpace;
    float3 color;
};

DrawAtomResult DrawAtom(float3 vertex, float3 normal, uint instanceID)
{
    DrawAtomResult result;

    float3 position = AllAtoms[instanceID].position;
    float3 velocity = AllAtoms[instanceID].velocity;
    float4 rotation = AllAtoms[instanceID].rotation;

    float4x4 rotationMatrix = quaternion_to_matrix_4x4(rotation);
    result.normalWorldSpace = mul(rotationMatrix, normal);

    result.vertexPositionWorldSpace = mul(rotationMatrix, vertex * AtomRadius * 2) + position;

    float debugValue = 0;//velocityData.w;
    float speed = saturate(length(velocity) / 3.0f);
    float neighbours = saturate(debugValue / 20.0f);
    result.color = float3(speed, (1.5 - neighbours) * (1.1 - speed), 0.1 + neighbours);

    return result;
}

struct DrawHalfBondResult
{
    float3 vertexPositionWorldSpace;
    float3 normalWorldSpace;
    float3 color;
};


DrawHalfBondResult DrawHalfBond(float3 vertex, float3 normal, uint instanceID)
{
    DrawHalfBondResult result;

    uint atomIndex = instanceID / HALF_BOND_MAX_COUNT;
    float3 position = AllAtoms[atomIndex].position;
    float4 rotation = AllAtoms[atomIndex].rotation;
    float3 velocity = AllAtoms[atomIndex].velocity;
    
    float3 direction = AllHalfBonds[instanceID].directionLocalSpace;

    float3x3 rotationMatrix = quaternion_to_matrix_3x3(rotation);
    rotationMatrix = mul(rotationMatrix, rotation_matrix_3x3(direction, float3(0,1,0)));

    result.normalWorldSpace = mul(rotationMatrix, normal);

    result.vertexPositionWorldSpace = mul(rotationMatrix, vertex * float3(0.05, 0.05, AtomRadius * 5)) + position;

    float energy = AllHalfBonds[instanceID].energy;
    result.color = float3(-energy * 5, 0, 0);

    return result;
}

#endif