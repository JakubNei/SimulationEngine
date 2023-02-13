using UnityEngine;
using UnityEngine.Rendering;

// Generated from ParticleSimulation.compute
public struct ParticleSimulationCompute
{
	Vector3 kernelSize;
	ComputeShaderExecution execution;

	static bool computeShaderLoaded;
	static ComputeShader computeShaderInstance;
	public static ComputeShader ComputeShader
	{
		get
		{
			if (!computeShaderLoaded)
			{ 
				computeShaderInstance = Resources.Load<ComputeShader>("ParticleSimulation");
				if (computeShaderInstance)
					computeShaderLoaded = true;
			}
			return computeShaderInstance;
		}
	}


	#region Kernels that can be executed
	// Kernel UpdateLightAdjustment_Finish with [numthreads(8, 8, 1)]
	// void UpdateLightAdjustment_Finish(uint3 id : SV_DispatchThreadID)
	public static ParticleSimulationCompute UpdateLightAdjustment_Finish(CommandBuffer commandBuffer)
	{
		return UpdateLightAdjustment_Finish(commandBuffer, ComputeShader);
	}
	// Kernel UpdateLightAdjustment_Finish with [numthreads(8, 8, 1)]
	// void UpdateLightAdjustment_Finish(uint3 id : SV_DispatchThreadID)
	public static ParticleSimulationCompute UpdateLightAdjustment_Finish(CommandBuffer commandBuffer, ComputeShader computeShader)
	{
		return new ParticleSimulationCompute()
		{
			execution = ComputeShaderExecution.For(commandBuffer, computeShader, "UpdateLightAdjustment_Finish"),
			kernelSize = KernelSize.UpdateLightAdjustment_Finish,
		};
	}
	#endregion


	#region Include files
	#endregion


	#region All setttable uniforms including those from all include files
	// RWStructuredBuffer<float3> AllParticles_Position;
	public ComputeBuffer AllParticles_Position { set => execution.SetBufferParam(PropertyId.AllParticles_Position, value); }
	// RWStructuredBuffer<float3> AllParticles_Velocity;
	public ComputeBuffer AllParticles_Velocity { set => execution.SetBufferParam(PropertyId.AllParticles_Velocity, value); }
	// int AllParticles_Length;
	public int AllParticles_Length { set => execution.SetIntParam(PropertyId.AllParticles_Length, value); }
	// RWTexture2D<LIGHT_ADJUSTMENT_DATA_PACKED> LigthAdjustment;
	public RenderTargetIdentifier LigthAdjustment { set => execution.SetTextureParam(PropertyId.LigthAdjustment, value); }
	#endregion


	#region Structs
	#endregion


	#region Dispatch call with convenience parameters
	public void Dispatch(int x, int y = 1, int z = 1) => execution.Dispatch(Mathf.CeilToInt(x / kernelSize.x), Mathf.CeilToInt(y / kernelSize.y), Mathf.CeilToInt(z / kernelSize.z));
	public void Dispatch(uint x, uint y = 1, uint z = 1) => execution.Dispatch(Mathf.CeilToInt(x / kernelSize.x), Mathf.CeilToInt(y / kernelSize.y), Mathf.CeilToInt(z / kernelSize.z));
	public void Dispatch(ComputeBuffer indirectBuffer, uint argsOffset = 0) => execution.Dispatch(indirectBuffer, argsOffset);
	public void Dispatch(GraphicsBuffer indirectBuffer, uint argsOffset = 0) => execution.Dispatch(indirectBuffer, argsOffset);
	public void Dispatch(Vector2Int xy) => Dispatch(xy.x, xy.y);
	public void Dispatch(Vector3Int xyz) => Dispatch(xyz.x, xyz.y, xyz.z);
	#endregion


	#region Kernel sizes
	public static class KernelSize
	{
		public static readonly Vector3Int UpdateLightAdjustment_Finish = new Vector3Int(8, 8, 1);
	}
	#endregion


	#region Hashed property ids
	public static class PropertyId
	{
		public static readonly int AllParticles_Position;
		public static readonly int AllParticles_Velocity;
		public static readonly int AllParticles_Length;
		public static readonly int LigthAdjustment;
		static PropertyId()
		{
			AllParticles_Position = Shader.PropertyToID("AllParticles_Position");
			AllParticles_Velocity = Shader.PropertyToID("AllParticles_Velocity");
			AllParticles_Length = Shader.PropertyToID("AllParticles_Length");
			LigthAdjustment = Shader.PropertyToID("LigthAdjustment");
		}
	}
	#endregion


}
