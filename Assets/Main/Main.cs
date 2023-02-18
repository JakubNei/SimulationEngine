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
	ComputeShader ConfigComputeShader;

	int AllParticles_Length;
	ComputeBuffer AllParticles_Position;
	ComputeBuffer AllParticles_Velocity;

	// index count per instance, instance count, start index location, base vertex location, start instance location
	ComputeBuffer IndirectArguments_DrawMeshParticles;

	// indexes of particles in AllParticles_Position, sorted by their position hashcode
	ComputeBuffer SortedParticleIndexes;

	// a way to find all particles with the same hashcode from hashcode
	// [first particle index in SortedParticleIndexes, num particles] *  HashCodeToSortedParticleIndexes_Length
	ComputeBuffer HashCodeToSortedParticleIndexes;
	// maximum amount of voxel cells
	int HashCodeToSortedParticleIndexes_Length;

	// our scale space is in nanometers, atoms have an average radius of about 0.1 nm, so one Unity unit is one nanometer in this project
	const float particleRadius = 0.1f;
	const float interactionMaxRadius = particleRadius * 2;
	float VoxelCellEdgeSize = interactionMaxRadius;

	struct CursorHitResult
	{
		public int particleIndex;
		public Vector3 worldPosition;
	}

	List<CursorHitResult> lastCursorHitResults = new();
	CursorHitResult? lastClosestCursorHitResult;

	List<int> emptyHitResult = new(1024);
	Queue<ComputeBuffer> hitResultPool = new();

	List<float> emptyFetchParticlePosition = new(4);
	Queue<ComputeBuffer> fetchParticlePositionPool = new();

	uint? DraggingParticleIndex;
	Vector3? DraggingParticleWorldPos;


	// Start is called before the first frame update
	void Start()
	{
		// ERROR: Thread group count is above the maximum allowed limit. Maximum allowed thread group count is 65535

		//AllParticles_Length = 128 * 128 * 64; // 1 million
		//AllParticles_Length = 1024 * 64;
		AllParticles_Length = 64 * 64;

		HashCodeToSortedParticleIndexes_Length = 1024 * 64;

		HashCodeToSortedParticleIndexes = new ComputeBuffer(HashCodeToSortedParticleIndexes_Length * 2, Marshal.SizeOf(typeof(uint)), ComputeBufferType.Structured);

		SortedParticleIndexes = new ComputeBuffer(AllParticles_Length, Marshal.SizeOf(typeof(uint)), ComputeBufferType.Structured);
		{
			var indexes = new List<uint>(AllParticles_Length);
			for (uint i = 0; i < AllParticles_Length; i++)
			{
				indexes.Add(i);
			}
			SortedParticleIndexes.SetData(indexes);
		}

		AllParticles_Position = new ComputeBuffer(AllParticles_Length, Marshal.SizeOf(typeof(float)) * 4, ComputeBufferType.Structured);
		var positions = new List<float>(AllParticles_Length * 3);

		{
			var countOnEdge = Mathf.CeilToInt(Mathf.Pow(AllParticles_Length, 1 / 2.0f));
			var gridSizeOnEdge = interactionMaxRadius * countOnEdge;
			Random.InitState(0x6584f86);
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

		IndirectArguments_DrawMeshParticles = new ComputeBuffer(5, sizeof(int), ComputeBufferType.IndirectArguments);
		IndirectArguments_DrawMeshParticles.SetData(new uint[] { ConfigParticleMesh.GetIndexCount(0), (uint)AllParticles_Length, ConfigParticleMesh.GetIndexStart(0), ConfigParticleMesh.GetBaseVertex(0), 0 });

		for (int i = 0; i < emptyHitResult.Capacity; i++)
			emptyHitResult.Add(0);

		for (int i = 0; i < emptyFetchParticlePosition.Capacity; i++)
			emptyFetchParticlePosition.Add(0);
	}

	// Update is called once per frame
	void Update()
	{
		{
			var bitonicSort = ConfigComputeShader.FindKernel("BitonicSort");
			ConfigComputeShader.SetFloat("VoxelCellEdgeSize", VoxelCellEdgeSize);
			ConfigComputeShader.SetInt("HashCodeToSortedParticleIndexes_Length", HashCodeToSortedParticleIndexes_Length);
			ConfigComputeShader.SetBuffer(bitonicSort, "AllParticles_Position", AllParticles_Position);
			ConfigComputeShader.SetBuffer(bitonicSort, "AllParticles_Velocity", AllParticles_Velocity);
			ConfigComputeShader.SetBuffer(bitonicSort, "SortedParticleIndexes", SortedParticleIndexes);
			for (int DirectionChangeStride = 2; DirectionChangeStride <= AllParticles_Length; DirectionChangeStride *= 2)
			{
				for (int ComparisonOffset = DirectionChangeStride / 2; true; ComparisonOffset /= 2)
				{
					ConfigComputeShader.SetInt("DirectionChangeStride", DirectionChangeStride);
					ConfigComputeShader.SetInt("ComparisonOffset", ComparisonOffset);
					ConfigComputeShader.Dispatch(bitonicSort, AllParticles_Length / 64, 1, 1);
					if (ComparisonOffset == 1) break;
				}
			}
		}

		{
			{
				var HashCodeToSortedParticleIndexes_Initialize = ConfigComputeShader.FindKernel("HashCodeToSortedParticleIndexes_Initialize");
				ConfigComputeShader.SetBuffer(HashCodeToSortedParticleIndexes_Initialize, "HashCodeToSortedParticleIndexes", HashCodeToSortedParticleIndexes);
				ConfigComputeShader.Dispatch(HashCodeToSortedParticleIndexes_Initialize, HashCodeToSortedParticleIndexes_Length / 64, 1, 1);
			}

			{
				var HashCodeToSortedParticleIndexes_Bin = ConfigComputeShader.FindKernel("HashCodeToSortedParticleIndexes_Bin");
				ConfigComputeShader.SetBuffer(HashCodeToSortedParticleIndexes_Bin, "AllParticles_Position", AllParticles_Position);
				ConfigComputeShader.SetFloat("VoxelCellEdgeSize", VoxelCellEdgeSize);
				ConfigComputeShader.SetBuffer(HashCodeToSortedParticleIndexes_Bin, "HashCodeToSortedParticleIndexes", HashCodeToSortedParticleIndexes);
				ConfigComputeShader.SetInt("HashCodeToSortedParticleIndexes_Length", HashCodeToSortedParticleIndexes_Length);
				ConfigComputeShader.SetBuffer(HashCodeToSortedParticleIndexes_Bin, "SortedParticleIndexes", SortedParticleIndexes);
				ConfigComputeShader.Dispatch(HashCodeToSortedParticleIndexes_Bin, AllParticles_Length / 64, 1, 1);
			}
		}

		{
			if (!hitResultPool.TryDequeue(out var hitResult))
				hitResult = new ComputeBuffer(emptyHitResult.Count, Marshal.SizeOf(typeof(int)) * 4, ComputeBufferType.Structured);
			hitResult.SetData(emptyHitResult);

			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			var raycastHitParticles = ConfigComputeShader.FindKernel("RaycastHitParticles");
			ConfigComputeShader.SetFloat("ParticleRadius", particleRadius);
			ConfigComputeShader.SetVector("RayStartWorldPos", ray.origin);
			ConfigComputeShader.SetVector("RayDirection", ray.direction);
			ConfigComputeShader.SetBuffer(raycastHitParticles, "HitResults", hitResult);
			ConfigComputeShader.SetBuffer(raycastHitParticles, "AllParticles_Position", AllParticles_Position);
			ConfigComputeShader.Dispatch(raycastHitParticles, AllParticles_Length / 64, 1, 1);

			AsyncGPUReadback.Request(hitResult, (result) =>
			{
				var data = result.GetData<int>();
				lastCursorHitResults.Clear();
				for (int x = 0; x < data[0]; x++)
				{
					// TODO use struct from ComputeShader
					lastCursorHitResults.Add(new CursorHitResult()
					{
						particleIndex = data[(x + 1) * 4 + 0],
						worldPosition = new Vector3(
							data[(x + 1) * 4 + 1] / 1000.0f,
							data[(x + 1) * 4 + 2] / 1000.0f,
							data[(x + 1) * 4 + 3] / 1000.0f
						)
					});
				}

				lastClosestCursorHitResult = null;
				var closestDistance = float.MaxValue;
				for (int i = 0; i < lastCursorHitResults.Count; i++)
				{
					var distance = Vector3.Distance(ray.origin, lastCursorHitResults[i].worldPosition);
					if (distance < closestDistance)
					{
						closestDistance = distance;
						lastClosestCursorHitResult = lastCursorHitResults[i];
					}
				}

				hitResultPool.Enqueue(hitResult);
			});
		}

		// drag
		{
			if (DraggingParticleIndex.HasValue)
			{
				if (!Input.GetKey(KeyCode.Mouse0))
				{
					// exit drag
					DraggingParticleIndex = null;
					ConfigComputeShader.SetInt("DragParticleIndex", -1);
				}
				else
				{
					// tick drag
					ReadbackParticlePosition(DraggingParticleIndex.Value, (worldPosition) =>
					{
						DraggingParticleWorldPos = worldPosition;
					});
					var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
					var dragPlane = new Plane(Camera.main.transform.forward, DraggingParticleWorldPos.Value);
					if (dragPlane.Raycast(ray, out float enter))
					{
						var dragTargetPosition = ray.origin + ray.direction * enter;
						ConfigComputeShader.SetInt("DragParticleIndex", (int)DraggingParticleIndex);
						ConfigComputeShader.SetVector("DragTargetWorldPosition", dragTargetPosition);
						SimpleDraw.Game.Line(DraggingParticleWorldPos.Value, dragTargetPosition, Color.white);
					}
					else
					{
						// exit drag
						DraggingParticleIndex = null;
						ConfigComputeShader.SetInt("DragParticleIndex", -1);
					}
				}
			}
			else if (Input.GetKeyDown(KeyCode.Mouse0) && lastClosestCursorHitResult.HasValue)
			{
				// start drag
				DraggingParticleIndex = (uint)lastClosestCursorHitResult.Value.particleIndex;
				DraggingParticleWorldPos = lastClosestCursorHitResult.Value.worldPosition;
			}
			else if (lastClosestCursorHitResult.HasValue)
			{
				// highlight particle to drag
				SimpleDraw.Game.Circle(
					lastClosestCursorHitResult.Value.worldPosition,
					Quaternion.LookRotation((lastClosestCursorHitResult.Value.worldPosition - Camera.main.transform.position).normalized, Camera.main.transform.up),
					Color.white, particleRadius * 1.4f);
			}
		}

		{
			var simulate = ConfigComputeShader.FindKernel("Simulate");
			ConfigComputeShader.SetFloat("DeltaTime", 0.01f);
			ConfigComputeShader.SetBuffer(simulate, "AllParticles_Position", AllParticles_Position);
			ConfigComputeShader.SetBuffer(simulate, "AllParticles_Velocity", AllParticles_Velocity);
			ConfigComputeShader.SetFloat("VoxelCellEdgeSize", VoxelCellEdgeSize);
			ConfigComputeShader.SetBuffer(simulate, "HashCodeToSortedParticleIndexes", HashCodeToSortedParticleIndexes);
			ConfigComputeShader.SetInt("HashCodeToSortedParticleIndexes_Length", HashCodeToSortedParticleIndexes_Length);
			ConfigComputeShader.SetBuffer(simulate, "SortedParticleIndexes", SortedParticleIndexes);
			ConfigComputeShader.Dispatch(simulate, AllParticles_Length / 64, 1, 1);
		}

		DrawParticles();
	}

	void DrawParticles()
	{
		var material = ConfigParticlesMaterial;

		material.SetInt("AllParticles_Length", AllParticles_Length);
		material.SetBuffer("AllParticles_Position", AllParticles_Position);
		material.SetBuffer("AllParticles_Velocity", AllParticles_Velocity);
		material.SetBuffer("HashCodeToSortedParticleIndexes", HashCodeToSortedParticleIndexes);
		material.SetInt("HashCodeToSortedParticleIndexes_Length", HashCodeToSortedParticleIndexes_Length);
		material.SetFloat("Scale", particleRadius * 2);
		material.SetFloat("VoxelCellEdgeSize", VoxelCellEdgeSize);

		var bounds = new Bounds(Vector3.zero, new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));
		Graphics.DrawMeshInstancedIndirect(
			ConfigParticleMesh, 0, material, bounds, IndirectArguments_DrawMeshParticles,
			0, null, ShadowCastingMode.Off, false, 0, null, LightProbeUsage.Off);
		// Graphics.DrawMeshInstancedIndirect(
		// 	ConfigParticleMesh, 0, material, bounds, Gpu_IndirectArguments_DrawMeshParticles,
		// 	0, null, ShadowCastingMode.On, true, 0, null, LightProbeUsage.BlendProbes);
	}

	void ReadbackParticlePosition(uint particleIndex, System.Action<Vector3> resultCallback)
	{
		AsyncGPUReadback.Request(AllParticles_Position, AllParticles_Position.stride, AllParticles_Position.stride * (int)particleIndex, (result) =>
		{
			var data = result.GetData<float>();
			resultCallback?.Invoke(new Vector3(data[0], data[1], data[2]));
		});
	}
}
