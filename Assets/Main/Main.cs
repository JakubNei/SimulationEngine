using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Main : MonoBehaviour
{
	[SerializeField]
	Material ConfigParticlesMaterial;
	[SerializeField]
	Mesh ConfigParticleMesh;

	int Gpu_AllParticles_Length;
	ComputeBuffer Gpu_AllParticles_Position;
	ComputeBuffer Gpu_AllParticles_Velocity;

	// index count per instance, instance count, start index location, base vertex location, start instance location
	ComputeBuffer Gpu_IndirectArguments_DrawMeshParticles;

	const float interactionMaxRadius = 1;
	const float particleRadius = 1f;

	// Start is called before the first frame update
	void Start()
	{
		// ERROR: Thread group count is above the maximum allowed limit. Maximum allowed thread group count is 65535
		//Gpu_AllParticles_Length = 65535 * 64;
		Gpu_AllParticles_Length = 65535;

		Gpu_AllParticles_Position = new ComputeBuffer(Gpu_AllParticles_Length, 4 * 3, ComputeBufferType.Structured);
		var positions = new List<float>(Gpu_AllParticles_Length * 3);
		// our scale space is in nanometers, atoms have an average radius of about 0.1 nm, so one Unity unit is one nanometer in this project
		var countOnEdge = Mathf.CeilToInt(Mathf.Pow(Gpu_AllParticles_Length, 0.33f));
		var gridSizeOnEdge = interactionMaxRadius * countOnEdge;
		for (int i = 0; i < Gpu_AllParticles_Length; i++)
		{
			var xRatio = (i % countOnEdge) / (float)countOnEdge;
			var yRatio = ((i / (float)countOnEdge) % countOnEdge) / (float)countOnEdge;
			var zRatio = ((i / (float)(countOnEdge * countOnEdge)) % countOnEdge) / (float)countOnEdge;
			positions.Add(xRatio * gridSizeOnEdge);
			positions.Add(yRatio * gridSizeOnEdge);
			positions.Add(zRatio * gridSizeOnEdge);
		}
		Gpu_AllParticles_Position.SetData(positions);

		Gpu_AllParticles_Velocity = new ComputeBuffer(Gpu_AllParticles_Length, 4 * 3, ComputeBufferType.Structured);

		Gpu_IndirectArguments_DrawMeshParticles = new ComputeBuffer(5, sizeof(int), ComputeBufferType.IndirectArguments);
		Gpu_IndirectArguments_DrawMeshParticles.SetData(new uint[] { ConfigParticleMesh.GetIndexCount(0), (uint)Gpu_AllParticles_Length, ConfigParticleMesh.GetIndexStart(0), ConfigParticleMesh.GetBaseVertex(0), 0 });
	}

	// Update is called once per frame
	void Update()
	{
		DrawParticles();
	}

	void DrawParticles()
	{
		var material = ConfigParticlesMaterial;

		material.SetInt("AllParticles_Length", Gpu_AllParticles_Length);
		material.SetBuffer("AllParticles_Position", Gpu_AllParticles_Position);
		material.SetBuffer("AllParticles_Velocity", Gpu_AllParticles_Velocity);
		material.SetFloat("Scale", particleRadius);

		var bounds = new Bounds(Vector3.zero, new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));
		Graphics.DrawMeshInstancedIndirect(
			ConfigParticleMesh, 0, material, bounds, Gpu_IndirectArguments_DrawMeshParticles,
			0, null, ShadowCastingMode.Off, false, 0, null, LightProbeUsage.Off);
		// Graphics.DrawMeshInstancedIndirect(
		// 	ConfigParticleMesh, 0, material, bounds, Gpu_IndirectArguments_DrawMeshParticles,
		// 	0, null, ShadowCastingMode.On, true, 0, null, LightProbeUsage.BlendProbes);
	}
}
