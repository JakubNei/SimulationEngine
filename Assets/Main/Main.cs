using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

public class Main : MonoBehaviour
{
	[SerializeField]
	Material ConfigParticlesMaterial;
	[SerializeField]
	Mesh ConfigParticleMesh;

	[SerializeField]
	ComputeShader computeShader;

	int AllParticles_Length;
	ComputeBuffer AllParticles_Position;
	ComputeBuffer AllParticles_Velocity;

	// index count per instance, instance count, start index location, base vertex location, start instance location
	ComputeBuffer Gpu_IndirectArguments_DrawMeshParticles;

	// [first particle index in AllParticles_Position, num particles] *  HashCodeToParticles_Length
	ComputeBuffer HashCodeToParticles;
	int HashCodeToParticles_Length;

	// our scale space is in nanometers, atoms have an average radius of about 0.1 nm, so one Unity unit is one nanometer in this project
	const float particleRadius = 0.1f;
	const float interactionMaxRadius = particleRadius * 2;
	float VoxelCellEdgeSize = interactionMaxRadius;

	// Start is called before the first frame update
	void Start()
	{
		// ERROR: Thread group count is above the maximum allowed limit. Maximum allowed thread group count is 65535

		HashCodeToParticles_Length = 1024 * 64;
		HashCodeToParticles = new ComputeBuffer(HashCodeToParticles_Length * 2, Marshal.SizeOf(typeof(uint)), ComputeBufferType.Structured);

		AllParticles_Length = 64 * 64;


		AllParticles_Position = new ComputeBuffer(AllParticles_Length, Marshal.SizeOf(typeof(float)) * 4, ComputeBufferType.Structured);
		var positions = new List<float>(AllParticles_Length * 3);

		{
			var countOnEdge = Mathf.CeilToInt(Mathf.Pow(AllParticles_Length, 1 / 2.0f));
			var gridSizeOnEdge = interactionMaxRadius * countOnEdge;
			{
				int i = 0;
				for (int x = 0; i < AllParticles_Length && x < countOnEdge; x++)
					for (int y = 0; i < AllParticles_Length && y < countOnEdge; y++)
					{
						var p = new Vector3(x * interactionMaxRadius, y * interactionMaxRadius, 0);
						p += Random.onUnitSphere * interactionMaxRadius;
						p.z = 0;

						positions.Add(p.x);
						positions.Add(p.y);
						positions.Add(p.z);
						positions.Add(0);
						i++;
					}
			}
		}
		AllParticles_Position.SetData(positions);

		AllParticles_Velocity = new ComputeBuffer(AllParticles_Length, Marshal.SizeOf(typeof(float)) * 4, ComputeBufferType.Structured);
		var velocities = new List<float>(AllParticles_Length * 3);
		for (int i = 0; i < AllParticles_Length; i++)
		{
			//var v = Random.insideUnitSphere * 0.1f;
			var v = Vector3.zero;
			v.z = 0;
			velocities.Add(v.x);
			velocities.Add(v.y);
			velocities.Add(v.z);
			velocities.Add(0);
		}
		AllParticles_Velocity.SetData(velocities);

		Gpu_IndirectArguments_DrawMeshParticles = new ComputeBuffer(5, sizeof(int), ComputeBufferType.IndirectArguments);
		Gpu_IndirectArguments_DrawMeshParticles.SetData(new uint[] { ConfigParticleMesh.GetIndexCount(0), (uint)AllParticles_Length, ConfigParticleMesh.GetIndexStart(0), ConfigParticleMesh.GetBaseVertex(0), 0 });


	}

	// Update is called once per frame
	void Update()
	{
		{
			var bitonicSort = computeShader.FindKernel("BitonicSort");
			computeShader.SetFloat("VoxelCellEdgeSize", VoxelCellEdgeSize);
			computeShader.SetInt("HashCodeToParticles_Length", HashCodeToParticles_Length);
			computeShader.SetBuffer(bitonicSort, "AllParticles_Position", AllParticles_Position);
			computeShader.SetBuffer(bitonicSort, "AllParticles_Velocity", AllParticles_Velocity);
			for (int DirectionChangeStride = 2; DirectionChangeStride <= AllParticles_Length; DirectionChangeStride *= 2)
			{
				for (int ComparisonOffset = DirectionChangeStride / 2; true; ComparisonOffset /= 2)
				{
					computeShader.SetInt("DirectionChangeStride", DirectionChangeStride);
					computeShader.SetInt("ComparisonOffset", ComparisonOffset);
					computeShader.Dispatch(bitonicSort, AllParticles_Length / 64, 1, 1);
					if (ComparisonOffset == 1) break;
				}
			}
		}

		{
			{
				var hashCodeToParticles_Initialize = computeShader.FindKernel("HashCodeToParticles_Initialize");
				computeShader.SetBuffer(hashCodeToParticles_Initialize, "HashCodeToParticles", HashCodeToParticles);
				computeShader.Dispatch(hashCodeToParticles_Initialize, HashCodeToParticles_Length / 64, 1, 1);
			}

			{
				var hashCodeToParticles_Bin = computeShader.FindKernel("HashCodeToParticles_Bin");
				computeShader.SetBuffer(hashCodeToParticles_Bin, "AllParticles_Position", AllParticles_Position);
				computeShader.SetFloat("VoxelCellEdgeSize", VoxelCellEdgeSize);
				computeShader.SetBuffer(hashCodeToParticles_Bin, "HashCodeToParticles", HashCodeToParticles);
				computeShader.SetInt("HashCodeToParticles_Length", HashCodeToParticles_Length);
				computeShader.Dispatch(hashCodeToParticles_Bin, AllParticles_Length / 64, 1, 1);
			}
		}

		{
			var simulate = computeShader.FindKernel("Simulate");
			computeShader.SetFloat("DeltaTime", Time.deltaTime);
			computeShader.SetBuffer(simulate, "AllParticles_Position", AllParticles_Position);
			computeShader.SetBuffer(simulate, "AllParticles_Velocity", AllParticles_Velocity);
			computeShader.SetFloat("VoxelCellEdgeSize", VoxelCellEdgeSize);
			computeShader.SetBuffer(simulate, "HashCodeToParticles", HashCodeToParticles);
			computeShader.SetInt("HashCodeToParticles_Length", HashCodeToParticles_Length);
			computeShader.Dispatch(simulate, AllParticles_Length / 64, 1, 1);
		}

		DrawParticles();
	}

	void DrawParticles()
	{
		var material = ConfigParticlesMaterial;

		material.SetInt("AllParticles_Length", AllParticles_Length);
		material.SetBuffer("AllParticles_Position", AllParticles_Position);
		material.SetBuffer("AllParticles_Velocity", AllParticles_Velocity);
		material.SetBuffer("HashCodeToParticles", HashCodeToParticles);
		material.SetInt("HashCodeToParticles_Length", HashCodeToParticles_Length);
		material.SetFloat("Scale", particleRadius * 2);
		material.SetFloat("VoxelCellEdgeSize", VoxelCellEdgeSize);

		var bounds = new Bounds(Vector3.zero, new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));
		Graphics.DrawMeshInstancedIndirect(
			ConfigParticleMesh, 0, material, bounds, Gpu_IndirectArguments_DrawMeshParticles,
			0, null, ShadowCastingMode.Off, false, 0, null, LightProbeUsage.Off);
		// Graphics.DrawMeshInstancedIndirect(
		// 	ConfigParticleMesh, 0, material, bounds, Gpu_IndirectArguments_DrawMeshParticles,
		// 	0, null, ShadowCastingMode.On, true, 0, null, LightProbeUsage.BlendProbes);
	}
}
