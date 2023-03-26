using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

public class Main : MonoBehaviour
{
	[SerializeField]
	Material ConfigAtomsMaterial;
	[SerializeField]
	Material ConfigAtomsMaterialWithShadows;
	[SerializeField]
	Mesh ConfigAtomMesh;

	[SerializeField]
	ComputeShader ConfigComputeShader;

	[SerializeField]
	GameObject ConfigBoundingPlanes;

	//int AllAtoms_Length = 128 * 128 * 64; // 1 million
	//int AllAtoms_Length = 1024 * 64; // 65k
	int AllAtoms_Length = 2 * 64 * 64;

	ComputeBuffer AllAtoms_Position;
	ComputeBuffer AllAtoms_Velocity;
	ComputeBuffer AllAtoms_Rotation;

	// index count per instance, instance count, start index location, base vertex location, start instance location
	ComputeBuffer IndirectArguments_DrawMeshAtoms;

	// pairs of [index to in AllAtoms_Position, position hashcode], sorted by their position hashcode
	ComputeBuffer SortedAtomIndexes;

	// a way to find all atoms with the same hashcode from hashcode
	// [first atom index in SortedAtomIndexes, num atoms] *  HashCodeToSortedAtomIndexes_Length
	ComputeBuffer HashCodeToSortedAtomIndexes;

	int BoundingPlanes_Length;

	// [normal x, normal y, normal z, distance] * BoundingPlanes_Length
	ComputeBuffer BoundingPlanes_NormalDistance;

	// maximum amount of voxel cells
	int HashCodeToSortedAtomIndexes_Length = 256 * 256 * 256;

	// our scale space is in nanometers, atoms have an average radius of about 0.1 nm, so one Unity unit is one nanometer in this project
	const float atomRadius = 0.1f;
	const float interactionMaxRadius = atomRadius * 2;
	float AtomInteractionMaxRadius = interactionMaxRadius;

	public bool ShouldDrawAtoms = true;
	public bool ShouldDrawAtomsShadows = true;
	public bool ShouldClampAtomsToXyPlane = true;
	public bool ShouldRunBitonicSort = true;
	public bool ShouldUseBitonicSortGroupSharedMemory = true;
	public bool ShouldAllowPlayerAtomDrag = true;
	public bool ShouldRunSimulation = true;


	struct CursorHitResult
	{
		public int atomIndex;
		public Vector3 worldPosition;
	}

	List<CursorHitResult> lastCursorHitResults = new();
	CursorHitResult? lastClosestCursorHitResult;

	List<int> emptyHitResult = new(1024);
	Queue<ComputeBuffer> hitResultPool = new();

	List<float> emptyFetchAtomPosition = new(4);
	Queue<ComputeBuffer> fetchAtomPositionPool = new();

	uint? DraggingAtomIndex;
	Vector3? DraggingAtomWorldPos;




	[StructLayout(LayoutKind.Sequential)]
	public struct Atom
	{
		public Vector3 position; 
		public Vector4 rotation; 
		public Vector3 force; 
		public Vector3 torque; 
		public Vector3 omegas; 

		public float Epz;
		public float rbond0;
		public float aMorse;
		public float bMorse;

		public HalfBond halfBonds_0;
		public HalfBond halfBonds_1;
		public HalfBond halfBonds_2;
		public HalfBond halfBonds_3;
	};

	[StructLayout(LayoutKind.Sequential)]
	public struct HalfBond
	{
		public Vector3 directionWorldSpace;
		public Vector3 directionLocalSpace;
		public Vector3 force;
		public Vector3 energy;
		public int cap;
	};


	static Vector3[] sp3_hs = new [] {
		new Vector3(1.000000000000f,  0.00000000000f,  0.00000000000f),
		new Vector3(-0.33380685923f,  0.94264149109f,  0.00000000000f),
		new Vector3(-0.33380685923f, -0.47132074554f, -0.81635147794f),
		new Vector3(-0.33380685923f, -0.47132074554f, +0.81635147794f),
	};

	static Vector3[] sp2_hs = new [] {
		new Vector3(+1.000000000000f, -0.00000000000f,  0.00000000000f),
		new Vector3(-0.500000000000f, +0.86602540378f,  0.00000000000f),
		new Vector3(-0.500000000000f, -0.86602540378f,  0.00000000000f),
		new Vector3(0.00000000000f,   0.00000000000f,  1.00000000000f), // electron - can be repulsive
	};

	static Vector3[] sp1_hs = new [] {
		new Vector3(+1.000000000000f,  0.00000000000f,  0.00000000000f),
		new Vector3(-1.000000000000f,  0.00000000000f,  0.00000000000f),
		new Vector3(0.000000000000f,  1.00000000000f,  0.00000000000f), // electron - can be repulsive
		new Vector3(0.000000000000f,  0.00000000000f,  1.00000000000f), // electron - can be repulsive
	};

	// Start is called before the first frame update
	void Start()
	{
		Random.InitState(0x6584f86);

		Application.targetFrameRate = -1;

		// ERROR: Thread group count is above the maximum allowed limit. Maximum allowed thread group count is 65535

		// force 64 for num threads
		AllAtoms_Length = Mathf.Max(1, Mathf.CeilToInt(AllAtoms_Length / 512)) * 512;
		HashCodeToSortedAtomIndexes_Length = Mathf.Max(512, Mathf.CeilToInt(HashCodeToSortedAtomIndexes_Length / 512)) * 512;

		HashCodeToSortedAtomIndexes = new ComputeBuffer(HashCodeToSortedAtomIndexes_Length * 2, Marshal.SizeOf(typeof(uint)), ComputeBufferType.Structured);

		SortedAtomIndexes = new ComputeBuffer(AllAtoms_Length, Marshal.SizeOf(typeof(uint)) * 2, ComputeBufferType.Structured);

		AllAtoms_Position = new ComputeBuffer(AllAtoms_Length, Marshal.SizeOf(typeof(float)) * 4, ComputeBufferType.Structured);
		var positions = new List<float>(AllAtoms_Length * 3);

		{
			var countOnEdge = Mathf.CeilToInt(Mathf.Pow(AllAtoms_Length, 1 / 2.0f));
			var gridSizeOnEdge = interactionMaxRadius * countOnEdge;
			int i = 0;
			for (int x = 0; i < AllAtoms_Length && x < countOnEdge; x++)
				for (int y = 0; i < AllAtoms_Length && y < countOnEdge; y++)
				{
					var p = new Vector3(x * interactionMaxRadius, y * interactionMaxRadius, 0);
					p += Random.onUnitSphere * interactionMaxRadius;
					if (ShouldClampAtomsToXyPlane)
						p.z = 0;
					else
						p.z *= 0.1f;

					positions.Add(p.x);
					positions.Add(p.y);
					positions.Add(p.z);
					positions.Add(0);
					i++;
				}

		}
		AllAtoms_Position.SetData(positions);

		AllAtoms_Velocity = new ComputeBuffer(AllAtoms_Length, Marshal.SizeOf(typeof(float)) * 4, ComputeBufferType.Structured);
		var velocities = new List<float>(AllAtoms_Length * 3);
		for (int i = 0; i < AllAtoms_Length; i++)
		{
			//var v = Random.insideUnitSphere * 0.1f;
			var v = Vector3.zero;
			velocities.Add(v.x);
			velocities.Add(v.y);
			velocities.Add(v.z);
			velocities.Add(0);
		}
		AllAtoms_Velocity.SetData(velocities);


		AllAtoms_Rotation = new ComputeBuffer(AllAtoms_Length, Marshal.SizeOf(typeof(float)) * 4, ComputeBufferType.Structured);
		var rotations = new List<float>(AllAtoms_Length * 4);
		for (int i = 0; i < AllAtoms_Length; i++)
		{
			var v = Random.rotation;
			rotations.Add(v.x);
			rotations.Add(v.y);
			rotations.Add(v.z);
			rotations.Add(v.w);
		}
		AllAtoms_Rotation.SetData(rotations);


		IndirectArguments_DrawMeshAtoms = new ComputeBuffer(5, sizeof(int), ComputeBufferType.IndirectArguments);
		IndirectArguments_DrawMeshAtoms.SetData(new uint[] { ConfigAtomMesh.GetIndexCount(0), (uint)AllAtoms_Length, ConfigAtomMesh.GetIndexStart(0), ConfigAtomMesh.GetBaseVertex(0), 0 });

		emptyHitResult.Clear();
		for (int i = 0; i < 4; i++)
			emptyHitResult.Add(0);

		emptyFetchAtomPosition.Clear();
		for (int i = 0; i < 1024; i++)
			emptyFetchAtomPosition.Add(0);
	}

	void OnDestroy()
	{
		AllAtoms_Position?.Dispose();
		AllAtoms_Position = null;
		AllAtoms_Velocity?.Dispose();
		AllAtoms_Velocity = null;
		IndirectArguments_DrawMeshAtoms?.Dispose();
		IndirectArguments_DrawMeshAtoms = null;
		SortedAtomIndexes?.Dispose();
		SortedAtomIndexes = null;
		HashCodeToSortedAtomIndexes?.Dispose();
		HashCodeToSortedAtomIndexes = null;

		foreach (var c in hitResultPool)
			c.Dispose();
		hitResultPool.Clear();

		foreach (var c in fetchAtomPositionPool)
			c.Dispose();
		fetchAtomPositionPool.Clear();
	}


	void OnGUI()
	{
		GUILayout.Label("fps " + Mathf.RoundToInt(1.0f / Time.smoothDeltaTime));
		GUILayout.Label("num atoms " + AllAtoms_Length);
		if (GUILayout.Button("increase *2"))
		{
			AllAtoms_Length *= 2;
			OnDestroy();
			Start();
		}
		if (GUILayout.Button("decrease /2"))
		{
			AllAtoms_Length /= 2;
			OnDestroy();
			Start();
		}

		ShouldDrawAtoms = GUILayout.Toggle(ShouldDrawAtoms, "draw atoms");
		ShouldDrawAtomsShadows = GUILayout.Toggle(ShouldDrawAtomsShadows, "draw atoms shadows");
		ShouldAllowPlayerAtomDrag = GUILayout.Toggle(ShouldAllowPlayerAtomDrag, "allow player to drag atom");
		ShouldRunBitonicSort = GUILayout.Toggle(ShouldRunBitonicSort, "run bitonic sort");
		ShouldUseBitonicSortGroupSharedMemory = GUILayout.Toggle(ShouldUseBitonicSortGroupSharedMemory, "use bitonic sort group shared memory");
		ShouldRunSimulation = GUILayout.Toggle(ShouldRunSimulation, "run atom velocity and position update");
		ShouldClampAtomsToXyPlane = GUILayout.Toggle(ShouldClampAtomsToXyPlane, "clamp atoms to xy plane");

	}

	// Update is called once per frame
	void Update()
	{
		// update bounding planes to compute buffer, so we can change them during runtime in editor
		if (ConfigBoundingPlanes != null)
		{
			var childCount = ConfigBoundingPlanes.transform.childCount;
			var boundingPlanes = new List<float>(childCount * 4);
			for (int childIndex = 0; childIndex < childCount; childIndex++)
			{
				var child = ConfigBoundingPlanes.transform.GetChild(childIndex);
				var plane = new Plane(child.up, child.position);
				boundingPlanes.Add(plane.normal.x);
				boundingPlanes.Add(plane.normal.y);
				boundingPlanes.Add(plane.normal.z);
				boundingPlanes.Add(plane.distance);
			}
			if (BoundingPlanes_Length != childCount)
			{
				BoundingPlanes_Length = childCount;
				if (BoundingPlanes_NormalDistance != null)
					BoundingPlanes_NormalDistance.Release();
				BoundingPlanes_NormalDistance = new ComputeBuffer(BoundingPlanes_Length, Marshal.SizeOf(typeof(float)) * 4, ComputeBufferType.Structured);
			}
			BoundingPlanes_NormalDistance.SetData(boundingPlanes);
		}

		if (ShouldRunBitonicSort)
		{
			{
				var bitonicSort = ConfigComputeShader.FindKernel("BitonicSort_Initialize");
				ConfigComputeShader.SetFloat("AtomInteractionMaxRadius", AtomInteractionMaxRadius);
				ConfigComputeShader.SetInt("HashCodeToSortedAtomIndexes_Length", HashCodeToSortedAtomIndexes_Length);
				ConfigComputeShader.SetBuffer(bitonicSort, "AllAtoms_Position", AllAtoms_Position);
				ConfigComputeShader.SetBuffer(bitonicSort, "SortedAtomIndexes", SortedAtomIndexes);
				ConfigComputeShader.Dispatch(bitonicSort, AllAtoms_Length / 512, 1, 1);
			}

			{
				for (int DirectionChangeStride = 2; DirectionChangeStride <= AllAtoms_Length; DirectionChangeStride *= 2)
				{
					for (int ComparisonOffset = DirectionChangeStride / 2; true; ComparisonOffset /= 2)
					{
						if (ShouldUseBitonicSortGroupSharedMemory && ComparisonOffset <= 256)
						{
							var bitonicSort = ConfigComputeShader.FindKernel("BitonicSort_Sort_GroupShared");
							ConfigComputeShader.SetBuffer(bitonicSort, "SortedAtomIndexes", SortedAtomIndexes);
							ConfigComputeShader.SetInt("DirectionChangeStride", DirectionChangeStride);
							ConfigComputeShader.SetInt("ComparisonOffset", ComparisonOffset);
							ConfigComputeShader.Dispatch(bitonicSort, AllAtoms_Length / 512, 1, 1);
							break;
						}
						else
						{
							var bitonicSort = ConfigComputeShader.FindKernel("BitonicSort_Sort");
							ConfigComputeShader.SetBuffer(bitonicSort, "SortedAtomIndexes", SortedAtomIndexes);
							ConfigComputeShader.SetInt("DirectionChangeStride", DirectionChangeStride);
							ConfigComputeShader.SetInt("ComparisonOffset", ComparisonOffset);
							ConfigComputeShader.Dispatch(bitonicSort, AllAtoms_Length / 512, 1, 1);
							if (ComparisonOffset == 1) break;
						}
					}
				}
			}

			{
				{
					var hashCodeToSortedAtomIndexes_Initialize = ConfigComputeShader.FindKernel("HashCodeToSortedAtomIndexes_Initialize");
					ConfigComputeShader.SetBuffer(hashCodeToSortedAtomIndexes_Initialize, "HashCodeToSortedAtomIndexes", HashCodeToSortedAtomIndexes);
					ConfigComputeShader.Dispatch(hashCodeToSortedAtomIndexes_Initialize, HashCodeToSortedAtomIndexes_Length / 512, 1, 1);
				}

				{
					var hashCodeToSortedAtomIndexes_Bin = ConfigComputeShader.FindKernel("HashCodeToSortedAtomIndexes_Bin");
					ConfigComputeShader.SetBuffer(hashCodeToSortedAtomIndexes_Bin, "AllAtoms_Position", AllAtoms_Position);
					ConfigComputeShader.SetFloat("AtomInteractionMaxRadius", AtomInteractionMaxRadius);
					ConfigComputeShader.SetInt("AllAtoms_Length", AllAtoms_Length);
					ConfigComputeShader.SetBuffer(hashCodeToSortedAtomIndexes_Bin, "HashCodeToSortedAtomIndexes", HashCodeToSortedAtomIndexes);
					ConfigComputeShader.SetInt("HashCodeToSortedAtomIndexes_Length", HashCodeToSortedAtomIndexes_Length);
					ConfigComputeShader.SetBuffer(hashCodeToSortedAtomIndexes_Bin, "SortedAtomIndexes", SortedAtomIndexes);
					ConfigComputeShader.Dispatch(hashCodeToSortedAtomIndexes_Bin, AllAtoms_Length / 512, 1, 1);
				}
			}
		}

		// raycast find atom under mouse
		if (ShouldAllowPlayerAtomDrag)
		{
			if (!hitResultPool.TryDequeue(out var hitResult))
				hitResult = new ComputeBuffer(emptyHitResult.Count, Marshal.SizeOf(typeof(int)) * 4, ComputeBufferType.Structured);
			hitResult.SetData(emptyHitResult);

			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			var raycastHitAtoms = ConfigComputeShader.FindKernel("RaycastHitAtoms");
			ConfigComputeShader.SetFloat("AtomRadius", atomRadius);
			ConfigComputeShader.SetVector("RayStartWorldPos", ray.origin);
			ConfigComputeShader.SetVector("RayDirection", ray.direction);
			ConfigComputeShader.SetBuffer(raycastHitAtoms, "HitResults", hitResult);
			ConfigComputeShader.SetBuffer(raycastHitAtoms, "AllAtoms_Position", AllAtoms_Position);
			ConfigComputeShader.Dispatch(raycastHitAtoms, AllAtoms_Length / 512, 1, 1);

			AsyncGPUReadback.Request(hitResult, (result) =>
			{
				var data = result.GetData<int>();
				lastCursorHitResults.Clear();
				for (int x = 0; x < data[0] && (x + 1) * 4 < data.Length; x++)
				{
					// TODO use struct from ComputeShader
					lastCursorHitResults.Add(new CursorHitResult()
					{
						atomIndex = data[(x + 1) * 4 + 0],
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

		// drag atom
		if (ShouldAllowPlayerAtomDrag)
		{
			if (DraggingAtomIndex.HasValue)
			{
				if (!Input.GetKey(KeyCode.Mouse0))
				{
					// exit drag
					DraggingAtomIndex = null;
					ConfigComputeShader.SetInt("DragAtomIndex", -1);
				}
				else
				{
					// tick drag
					ReadbackAtomPosition(DraggingAtomIndex.Value, (worldPosition) =>
					{
						DraggingAtomWorldPos = worldPosition;
					});
					var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
					var dragPlane = new Plane(Camera.main.transform.forward, DraggingAtomWorldPos.Value);
					if (dragPlane.Raycast(ray, out float enter))
					{
						var dragTargetPosition = ray.origin + ray.direction * enter;
						ConfigComputeShader.SetInt("DragAtomIndex", (int)DraggingAtomIndex);
						ConfigComputeShader.SetVector("DragTargetWorldPosition", dragTargetPosition);
						SimpleDraw.Game.Line(DraggingAtomWorldPos.Value, dragTargetPosition, Color.white);
					}
					else
					{
						// exit drag
						DraggingAtomIndex = null;
						ConfigComputeShader.SetInt("DragAtomIndex", -1);
					}
				}
			}
			else if (Input.GetKeyDown(KeyCode.Mouse0) && lastClosestCursorHitResult.HasValue)
			{
				// start drag
				DraggingAtomIndex = (uint)lastClosestCursorHitResult.Value.atomIndex;
				DraggingAtomWorldPos = lastClosestCursorHitResult.Value.worldPosition;
			}
			else if (lastClosestCursorHitResult.HasValue)
			{
				// highlight atom to drag
				SimpleDraw.Game.Circle(
					lastClosestCursorHitResult.Value.worldPosition,
					Quaternion.LookRotation((lastClosestCursorHitResult.Value.worldPosition - Camera.main.transform.position).normalized, Camera.main.transform.up),
					Color.white, atomRadius * 1.4f);
			}
		}

		if (ShouldRunSimulation)
		{
			{
				var simulate = ConfigComputeShader.FindKernel("Simulate_Prepare");
				ConfigComputeShader.SetFloat("DeltaTime", 0.01f);
				ConfigComputeShader.SetFloat("AtomRadius", atomRadius);
				ConfigComputeShader.SetBuffer(simulate, "AllAtoms_Position", AllAtoms_Position);
				ConfigComputeShader.SetBuffer(simulate, "AllAtoms_Velocity", AllAtoms_Velocity);
				ConfigComputeShader.SetBuffer(simulate, "AllAtoms_Rotation", AllAtoms_Rotation);
				ConfigComputeShader.SetInt("BoundingPlanes_Length", BoundingPlanes_Length);
				ConfigComputeShader.SetBuffer(simulate, "BoundingPlanes_NormalDistance", BoundingPlanes_NormalDistance);
				ConfigComputeShader.SetFloat("AtomInteractionMaxRadius", AtomInteractionMaxRadius);
				ConfigComputeShader.SetBuffer(simulate, "HashCodeToSortedAtomIndexes", HashCodeToSortedAtomIndexes);
				ConfigComputeShader.SetInt("HashCodeToSortedAtomIndexes_Length", HashCodeToSortedAtomIndexes_Length);
				ConfigComputeShader.SetBuffer(simulate, "SortedAtomIndexes", SortedAtomIndexes);
				ConfigComputeShader.SetBool("ClampTo2D", ShouldClampAtomsToXyPlane);
				ConfigComputeShader.Dispatch(simulate, AllAtoms_Length / 512, 1, 1);
			}

			{
				var simulate = ConfigComputeShader.FindKernel("Simulate_EvaluateForces");
				ConfigComputeShader.SetFloat("DeltaTime", 0.01f);
				ConfigComputeShader.SetFloat("AtomRadius", atomRadius);
				ConfigComputeShader.SetBuffer(simulate, "AllAtoms_Position", AllAtoms_Position);
				ConfigComputeShader.SetBuffer(simulate, "AllAtoms_Velocity", AllAtoms_Velocity);
				ConfigComputeShader.SetBuffer(simulate, "AllAtoms_Rotation", AllAtoms_Rotation);
				ConfigComputeShader.SetInt("BoundingPlanes_Length", BoundingPlanes_Length);
				ConfigComputeShader.SetBuffer(simulate, "BoundingPlanes_NormalDistance", BoundingPlanes_NormalDistance);
				ConfigComputeShader.SetFloat("AtomInteractionMaxRadius", AtomInteractionMaxRadius);
				ConfigComputeShader.SetBuffer(simulate, "HashCodeToSortedAtomIndexes", HashCodeToSortedAtomIndexes);
				ConfigComputeShader.SetInt("HashCodeToSortedAtomIndexes_Length", HashCodeToSortedAtomIndexes_Length);
				ConfigComputeShader.SetBuffer(simulate, "SortedAtomIndexes", SortedAtomIndexes);
				ConfigComputeShader.SetBool("ClampTo2D", ShouldClampAtomsToXyPlane);
				ConfigComputeShader.Dispatch(simulate, AllAtoms_Length / 512, 1, 1);
			}

			{
				var simulate = ConfigComputeShader.FindKernel("Simulate_Move");
				ConfigComputeShader.SetFloat("DeltaTime", 0.01f);
				ConfigComputeShader.SetBuffer(simulate, "AllAtoms_Position", AllAtoms_Position);
				ConfigComputeShader.SetBuffer(simulate, "AllAtoms_Velocity", AllAtoms_Velocity);
				ConfigComputeShader.SetFloat("AtomInteractionMaxRadius", AtomInteractionMaxRadius);
				ConfigComputeShader.SetBuffer(simulate, "HashCodeToSortedAtomIndexes", HashCodeToSortedAtomIndexes);
				ConfigComputeShader.SetInt("HashCodeToSortedAtomIndexes_Length", HashCodeToSortedAtomIndexes_Length);
				ConfigComputeShader.SetBuffer(simulate, "SortedAtomIndexes", SortedAtomIndexes);
				ConfigComputeShader.SetBool("ClampTo2D", ShouldClampAtomsToXyPlane);
				ConfigComputeShader.Dispatch(simulate, AllAtoms_Length / 512, 1, 1);
			}
		}

		if (ShouldDrawAtoms)
			DrawAtoms();
	}

	void DrawAtoms()
	{
		var material = ShouldDrawAtomsShadows ? ConfigAtomsMaterialWithShadows : ConfigAtomsMaterial;

		material.SetInt("AllAtoms_Length", AllAtoms_Length);
		material.SetBuffer("AllAtoms_Position", AllAtoms_Position);
		material.SetBuffer("AllAtoms_Velocity", AllAtoms_Velocity);
		material.SetBuffer("AllAtoms_Rotation", AllAtoms_Rotation);
		material.SetBuffer("HashCodeToSortedAtomIndexes", HashCodeToSortedAtomIndexes);
		material.SetInt("HashCodeToSortedAtomIndexes_Length", HashCodeToSortedAtomIndexes_Length);
		material.SetFloat("Scale", atomRadius * 2);
		material.SetFloat("AtomInteractionMaxRadius", AtomInteractionMaxRadius);

		var bounds = new Bounds(Vector3.zero, Vector3.one * 1000);
		if (ShouldDrawAtomsShadows)
			Graphics.DrawMeshInstancedIndirect(ConfigAtomMesh, 0, material, bounds, IndirectArguments_DrawMeshAtoms, 0, null, ShadowCastingMode.On, true, 0, null, LightProbeUsage.BlendProbes);
		else
			Graphics.DrawMeshInstancedIndirect(ConfigAtomMesh, 0, material, bounds, IndirectArguments_DrawMeshAtoms, 0, null, ShadowCastingMode.Off, false, 0, null, LightProbeUsage.Off);
	}

	void ReadbackAtomPosition(uint atomIndex, System.Action<Vector3> resultCallback)
	{
		AsyncGPUReadback.Request(AllAtoms_Position, AllAtoms_Position.stride, AllAtoms_Position.stride * (int)atomIndex, (result) =>
		{
			var data = result.GetData<float>();
			resultCallback?.Invoke(new Vector3(data[0], data[1], data[2]));
		});
	}
}
