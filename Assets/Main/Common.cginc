



int HashCodeToParticles_Length;
float VoxelCellEdgeSize;

uint GetHashCode(float3 position)
{
    uint3 p = abs((int3)(position / VoxelCellEdgeSize));
    return (p.x + (p.y << 4) + (p.z << 8)) % HashCodeToParticles_Length;
}
