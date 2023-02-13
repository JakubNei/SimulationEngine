using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public struct ComputeShaderExecution
{
	public int kernelIndex;
	public CommandBuffer commandBuffer;
	public ComputeShader computeShader;

	public static ComputeShaderExecution For(CommandBuffer commandBuffer, ComputeShader computeShader, string kernelName)
	{
		return new ComputeShaderExecution()
		{
			kernelIndex = computeShader.FindKernel(kernelName),
			commandBuffer = commandBuffer,
			computeShader = computeShader
		};
	}

	#region Convenience methods

	public void Dispatch(uint threadGroupsX, uint threadGroupsY, uint threadGroupsZ) { Dispatch((int)threadGroupsX, (int)threadGroupsY, (int)threadGroupsZ); }

	private static int[] ToIntParams(Vector2Int value) => new int[] { value.x, value.y };
	private static int[] ToIntParams(Vector3Int value) => new int[] { value.x, value.y, value.z };
	public void SetVectorParam(string name, Vector2Int value) { SetIntParams(name, ToIntParams(value)); }
	public void SetVectorParam(int nameID, Vector2Int value) { SetIntParams(nameID, ToIntParams(value)); }
	public void SetVectorParam(string name, Vector3Int value) { SetIntParams(name, ToIntParams(value)); }
	public void SetVectorParam(int nameID, Vector3Int value) { SetIntParams(nameID, ToIntParams(value)); }

	private static float[] ToFloatParams(Vector2 value) => new float[] { value.x, value.y };
	private static float[] ToFloatParams(Vector3 value) => new float[] { value.x, value.y, value.z };
	public void SetVectorParam(string name, Vector2 value) { SetFloatParams(name, ToFloatParams(value)); }
	public void SetVectorParam(int nameID, Vector2 value) { SetFloatParams(nameID, ToFloatParams(value)); }
	public void SetVectorParam(string name, Vector3 value) { SetFloatParams(name, ToFloatParams(value)); }
	public void SetVectorParam(int nameID, Vector3 value) { SetFloatParams(nameID, ToFloatParams(value)); }

	private static int[] ToIntParams(uint[] values)
	{
		var result = new int[values.Length];
		for (int i = 0; i < result.Length; i++)
			result[i] = (int)values[i];
		return result;
	}

	public void SetIntParam(string name, params uint[] values) { SetIntParams(name, ToIntParams(values)); }
	public void SetIntParam(int nameID, params uint[] values) { SetIntParams(nameID, ToIntParams(values)); }
	public void SetIntParam(string name, uint value) { SetIntParam(name, (int)value); }
	public void SetIntParam(int nameID, uint value) { SetIntParam(nameID, (int)value); }

	private static int[] ToIntParams(bool[] values)
	{
		var result = new int[values.Length];
		for (int i = 0; i < result.Length; i++)
			result[i] = values[i] ? 1 : 0;
		return result;
	}

	public void SetBoolParam(string name, bool value) { SetIntParam(name, value ? 1 : 0); }
	public void SetBoolParam(int nameID, bool value) { SetIntParam(nameID, value ? 1 : 0); }
	public void SetBoolParam(string name, bool[] value) { SetIntParam(name, ToIntParams(value)); }
	public void SetBoolParam(int nameID, bool[] value) { SetIntParam(nameID, ToIntParams(value)); }

	public void SetFloatParam(string name, params float[] values) { SetFloatParam(name, values); }
	public void SetFloatParam(int nameID, params float[] values) { SetFloatParam(nameID, values); }
	public void SetIntParam(string name, params int[] values) { SetIntParam(name, values); }
	public void SetIntParam(int nameID, params int[] values) { SetIntParam(nameID, values); }

	#endregion


	#region Copy paste from UnityEngine.CommandBuffer API
	public void SetFloatParam(string name, float val) { commandBuffer.SetComputeFloatParam(computeShader, name, val); }
	public void SetFloatParam(int nameID, float val) { commandBuffer.SetComputeFloatParam(computeShader, nameID, val); }
	public void SetIntParam(string name, int val) { commandBuffer.SetComputeIntParam(computeShader, name, val); }
	public void SetIntParam(int nameID, int val) { commandBuffer.SetComputeIntParam(computeShader, nameID, val); }
	public void SetVectorParam(string name, Vector4 val) { commandBuffer.SetComputeVectorParam(computeShader, name, val); }
	public void SetVectorParam(int nameID, Vector4 val) { commandBuffer.SetComputeVectorParam(computeShader, nameID, val); }
	public void SetVectorArrayParam(string name, Vector4[] values) { commandBuffer.SetComputeVectorArrayParam(computeShader, name, values); }
	public void SetVectorArrayParam(int nameID, Vector4[] values) { commandBuffer.SetComputeVectorArrayParam(computeShader, nameID, values); }
	public void SetMatrixParam(string name, Matrix4x4 val) { commandBuffer.SetComputeMatrixParam(computeShader, name, val); }
	public void SetMatrixParam(int nameID, Matrix4x4 val) { commandBuffer.SetComputeMatrixParam(computeShader, nameID, val); }
	public void SetMatrixArrayParam(string name, Matrix4x4[] values) { commandBuffer.SetComputeMatrixArrayParam(computeShader, name, values); }
	public void SetMatrixArrayParam(int nameID, Matrix4x4[] values) { commandBuffer.SetComputeMatrixArrayParam(computeShader, nameID, values); }
	public void SetFloatParams(string name, params float[] values) { commandBuffer.SetComputeFloatParams(computeShader, name, values); }
	public void SetFloatParams(int nameID, params float[] values) { commandBuffer.SetComputeFloatParams(computeShader, nameID, values); }
	public void SetIntParams(string name, params int[] values) { commandBuffer.SetComputeIntParams(computeShader, name, values); }
	public void SetIntParams(int nameID, params int[] values) { commandBuffer.SetComputeIntParams(computeShader, nameID, values); }
	public void SetTextureParam(string name, RenderTargetIdentifier rt) { commandBuffer.SetComputeTextureParam(computeShader, kernelIndex, name, rt); }
	public void SetTextureParam(int nameID, RenderTargetIdentifier rt) { commandBuffer.SetComputeTextureParam(computeShader, kernelIndex, nameID, rt); }
	public void SetTextureParam(string name, RenderTargetIdentifier rt, int mipLevel) { commandBuffer.SetComputeTextureParam(computeShader, kernelIndex, name, rt, mipLevel); }
	public void SetTextureParam(int nameID, RenderTargetIdentifier rt, int mipLevel) { commandBuffer.SetComputeTextureParam(computeShader, kernelIndex, nameID, rt, mipLevel); }
	public void SetTextureParam(string name, RenderTargetIdentifier rt, int mipLevel, RenderTextureSubElement element) { commandBuffer.SetComputeTextureParam(computeShader, kernelIndex, name, rt, mipLevel, element); }
	public void SetTextureParam(int nameID, RenderTargetIdentifier rt, int mipLevel, RenderTextureSubElement element) { commandBuffer.SetComputeTextureParam(computeShader, kernelIndex, nameID, rt, mipLevel, element); }
	public void SetBufferParam(int nameID, ComputeBuffer buffer) { commandBuffer.SetComputeBufferParam(computeShader, kernelIndex, nameID, buffer); }
	public void SetBufferParam(string name, ComputeBuffer buffer) { commandBuffer.SetComputeBufferParam(computeShader, kernelIndex, name, buffer); }
	public void SetBufferParam(int nameID, GraphicsBuffer buffer) { commandBuffer.SetComputeBufferParam(computeShader, kernelIndex, nameID, buffer); }
	public void SetBufferParam(string name, GraphicsBuffer buffer) { commandBuffer.SetComputeBufferParam(computeShader, kernelIndex, name, buffer); }
	public void SetConstantBufferParam(int nameID, ComputeBuffer buffer, int offset, int size) { commandBuffer.SetComputeConstantBufferParam(computeShader, nameID, buffer, offset, size); }
	public void SetConstantBufferParam(string name, ComputeBuffer buffer, int offset, int size) { commandBuffer.SetComputeConstantBufferParam(computeShader, name, buffer, offset, size); }
	public void SetConstantBufferParam(int nameID, GraphicsBuffer buffer, int offset, int size) { commandBuffer.SetComputeConstantBufferParam(computeShader, nameID, buffer, offset, size); }
	public void SetConstantBufferParam(string name, GraphicsBuffer buffer, int offset, int size) { commandBuffer.SetComputeConstantBufferParam(computeShader, name, buffer, offset, size); }
	public void Dispatch(int threadGroupsX, int threadGroupsY, int threadGroupsZ) { commandBuffer.DispatchCompute(computeShader, kernelIndex, threadGroupsX, threadGroupsY, threadGroupsZ); }
	public void Dispatch(ComputeBuffer indirectBuffer, uint argsOffset) { commandBuffer.DispatchCompute(computeShader, kernelIndex, indirectBuffer, argsOffset); }
	public void Dispatch(GraphicsBuffer indirectBuffer, uint argsOffset) { commandBuffer.DispatchCompute(computeShader, kernelIndex, indirectBuffer, argsOffset); }
	#endregion


}
