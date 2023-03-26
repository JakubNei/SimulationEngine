

struct HalfBond
{	
	float3 directionWorldSpace; // direction of half-bond in world space, is calculated from directionLocalSpace before every step
    float3 directionLocalSpace; // direction of half-bond in local space relative to atom rotation
	float3 force; // world space force on half-bond-direction
	float3 energy; // bonding energy, not needed for movement, but can be used to visualise if the half-bond is bonded to something and how much
	int cap; // optimization, index of capping atom (if any), for example hydrogen, because hydrogen has only one bond, capping atom has different optimized code path
};

#define HALF_BOND_MAX_COUNT 4

struct Atom 
{
	float3 position; // world space position
	float4 rotation; // quaternion world space rotation
	float3 force; // world space force
	float3 torque; // world space torque
	float3 velocity;
    float3 omega; // used to dampen rotation change caused by torque

	float Epz;//    =  0.5; // pi-bond strenght
    float rbond0;// =  0.5; // bond lenght (sum of atomic radius)
    float aMorse;// = 4.0; // interaction energy (strenght) (A in E=A*exp(-b*r) )
    float bMorse;// = -0.7; // interaction decay strenght (b in E=A*exp(-b*r)  )

	int numUsedHalfBonds; // how many of the half-bonds atom has
};


