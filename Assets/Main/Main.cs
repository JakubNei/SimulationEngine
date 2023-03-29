using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

public class Main : MonoBehaviour
{

	[SerializeField]
	Mesh ConfigAtomMesh;
	[SerializeField]
	Material ConfigAtomsMaterialSimple;
	[SerializeField]
	Material ConfigAtomsMaterialStandartLit;


	[SerializeField]
	Mesh ConfigHalfBondMesh;
	[SerializeField]
	Material ConfigHalfBondMaterialSimple;
	[SerializeField]
	Material ConfigHalfBondMaterialStandartLit;



	[SerializeField]
	ComputeShader ConfigComputeShader;

	[SerializeField]
	GameObject ConfigBoundingPlanes;

	//int AllAtoms_Length = 128 * 128 * 64; // 1 million
	//int AllAtoms_Length = 1024 * 64; // 65k
	int AllAtoms_Length = 1024 * 64 * 2; // 131k
										 //int AllAtoms_Length = 2 * 64 * 64;

	ComputeBuffer AllAtoms;
	ComputeBuffer AllAtoms2;

	int AllHalfBonds_Length = 4;
	ComputeBuffer AllHalfBonds;
	ComputeBuffer AllHalfBonds2;

	// index count per instance, instance count, start index location, base vertex location, start instance location
	ComputeBuffer InstancedIndirectDraw_Atoms;

	ComputeBuffer InstancedIndirectDraw_HalfBonds;


	// pairs of [index to in AllAtoms_Position, position hashcode], sorted by their position hashcode
	ComputeBuffer SortedAtomIndexes;

	// a way to find all atoms with the same hashcode from hashcode
	// [first atom index in SortedAtomIndexes, num atoms] *  HashCodeToSortedAtomIndexes_Length
	ComputeBuffer HashCodeToSortedAtomIndexes;

	int BoundingPlanes_Length;

	// [normal x, normal y, normal z, distance] * BoundingPlanes_Length
	ComputeBuffer BoundingPlanes_NormalDistance;

	// maximum amount of voxel cells
	int HashCodeToSortedAtomIndexes_Length = 128 * 16 * 1024;

	// our scale space is in nanometers, atoms have an average radius of about 0.1 nm, so one Unity unit is one nanometer in this project
	const float atomRadius = 0.1f; // 0.1f;
	const float interactionMaxRadius = 5; // atomRadius * 2;
	float AtomInteractionMaxRadius = interactionMaxRadius;

	public bool DrawAtoms = true;
	public bool DrawAtomsWithStandartLit = false;
	public bool ClampAtomsToXyPlane = true;
	public bool RunBitonicSort = true;
	public bool UseBitonicSortGroupSharedMemory = true;
	public bool AllowPlayerAtomDrag = true;
	public bool ShouldRunSimulation = true;
	public bool RunSimulateEvaluateForces = true;
	public bool PreorderData = false;
	public bool UseZCurveHashCode = false;


	struct CursorHitResult
	{
		public int atomIndex;
		public Vector3 worldPosition;
	}

	List<CursorHitResult> lastCursorHitResults = new();
	CursorHitResult? lastClosestCursorHitResult;

	List<int> emptyHitResult = new(1024);
	Queue<ComputeBuffer> hitResultsPool = new();

	List<float> emptyFetchAtomPosition = new(4);
	Queue<ComputeBuffer> fetchAtomPositionPool = new();

	uint? DraggingAtomIndex;
	Vector3? DraggingAtomWorldPos;

	ComputeBuffer DispatchIndirect_Simulate_EvaluateForces;

	[StructLayout(LayoutKind.Sequential)]
	public struct Atom
	{
		public Vector4 position;
		public Quaternion rotation;
		public Vector4 force;
		public Vector4 torque;
		public Vector4 velocity;
		public Vector4 omegas;

		public float Epz;
		public float rbond0;
		public float aMorse;
		public float bMorse;

		public int numUsedHalfBonds;
	};

	[StructLayout(LayoutKind.Sequential)]
	public struct HalfBond
	{
		public Vector4 directionWorldSpace;
		public Vector4 directionLocalSpace;
		public Vector4 force;
		public Vector4 energy;
		public int cap;
	};

	static Vector3[] sp3_hs = new[] {
		new Vector3(1.000000000000f,  0.00000000000f,  0.00000000000f),
		new Vector3(-0.33380685923f,  0.94264149109f,  0.00000000000f),
		new Vector3(-0.33380685923f, -0.47132074554f, -0.81635147794f),
		new Vector3(-0.33380685923f, -0.47132074554f, +0.81635147794f),
	};

	static Vector3[] sp2_hs = new[] {
		new Vector3(+1.000000000000f, -0.00000000000f,  0.00000000000f),
		new Vector3(-0.500000000000f, +0.86602540378f,  0.00000000000f),
		new Vector3(-0.500000000000f, -0.86602540378f,  0.00000000000f),
		new Vector3(0.00000000000f,   0.00000000000f,  1.00000000000f), // electron - can be repulsive
	};

	static Vector3[] sp1_hs = new[] {
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

		int k = GetKernelSizex("Simulate_Prepare");
		AllAtoms_Length = Mathf.Max(1, Mathf.CeilToInt(AllAtoms_Length / k)) * k;
		AllHalfBonds_Length = AllAtoms_Length * 4;

		k = GetKernelSizex("HashCodeToSortedAtomIndexes_Initialize");
		HashCodeToSortedAtomIndexes_Length = Mathf.Max(1, Mathf.CeilToInt(HashCodeToSortedAtomIndexes_Length / k)) * k;

		HashCodeToSortedAtomIndexes = new ComputeBuffer(HashCodeToSortedAtomIndexes_Length * 2, Marshal.SizeOf(typeof(uint)), ComputeBufferType.Structured);

		SortedAtomIndexes = new ComputeBuffer(AllAtoms_Length, Marshal.SizeOf(typeof(uint)) * 2, ComputeBufferType.Structured);

		AllAtoms = new ComputeBuffer(AllAtoms_Length, Marshal.SizeOf(typeof(Atom)), ComputeBufferType.Structured);
		AllAtoms2 = new ComputeBuffer(AllAtoms_Length, Marshal.SizeOf(typeof(Atom)), ComputeBufferType.Structured);
		AllHalfBonds = new ComputeBuffer(AllHalfBonds_Length, Marshal.SizeOf(typeof(HalfBond)), ComputeBufferType.Structured);
		AllHalfBonds2 = new ComputeBuffer(AllHalfBonds_Length, Marshal.SizeOf(typeof(HalfBond)), ComputeBufferType.Structured);


		{
			var countOnEdge = Mathf.CeilToInt(Mathf.Pow(AllAtoms_Length, 1 / 2.0f));
			var gridSizeOnEdge = interactionMaxRadius * countOnEdge;
			int i = 0;
			var allAtoms = new List<Atom>(AllAtoms_Length);
			var allHalfBonds = new List<HalfBond>(AllHalfBonds_Length);
			for (int x = 0; i < AllAtoms_Length && x < countOnEdge; x++)
			{
				for (int y = 0; i < AllAtoms_Length && y < countOnEdge; y++)
				{
					var atom = new Atom();

					var p = new Vector3(x, y, 0) * interactionMaxRadius * 0.3f;
					p += Random.onUnitSphere * interactionMaxRadius * 0.2f;
					if (ClampAtomsToXyPlane)
						p.z = 0;
					atom.position = p;
					atom.velocity = Vector3.zero;
					atom.rotation = Random.rotation;
					atom.Epz = 0.5f;
					atom.rbond0 = 0.5f;
					atom.aMorse = 0.5f;
					atom.bMorse = -0.7f;
					atom.numUsedHalfBonds = 4;

					for (int h = 0; h < 4; h++)
					{
						allHalfBonds.Add(new HalfBond()
						{
							directionLocalSpace = sp3_hs[h],
							cap = -1,
						});
					}

					i++;
					allAtoms.Add(atom);
				}
			}
			AllAtoms.SetData(allAtoms);
			AllHalfBonds.SetData(allHalfBonds);
		}

		InstancedIndirectDraw_Atoms = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
		InstancedIndirectDraw_Atoms.SetData(new uint[] { ConfigAtomMesh.GetIndexCount(0), (uint)AllAtoms_Length, ConfigAtomMesh.GetIndexStart(0), ConfigAtomMesh.GetBaseVertex(0), 0 });

		InstancedIndirectDraw_HalfBonds = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
		InstancedIndirectDraw_HalfBonds.SetData(new uint[] { ConfigHalfBondMesh.GetIndexCount(0), (uint)AllHalfBonds_Length, ConfigHalfBondMesh.GetIndexStart(0), ConfigHalfBondMesh.GetBaseVertex(0), 0 });

		DispatchIndirect_Simulate_EvaluateForces = new ComputeBuffer(3, sizeof(uint), ComputeBufferType.IndirectArguments);
		DispatchIndirect_Simulate_EvaluateForces.SetData(new uint[] { (uint)AllAtoms_Length, 1, 1 });

		emptyHitResult.Clear();
		for (int i = 0; i < 4; i++)
			emptyHitResult.Add(0);

		emptyFetchAtomPosition.Clear();
		for (int i = 0; i < 1024; i++)
			emptyFetchAtomPosition.Add(0);
	}

	void OnDestroy()
	{
		AllAtoms?.Dispose();
		AllAtoms = null;
		AllAtoms2?.Dispose();
		AllAtoms2 = null;
		AllHalfBonds?.Dispose();
		AllHalfBonds = null;
		AllHalfBonds2?.Dispose();
		AllHalfBonds2 = null;
		InstancedIndirectDraw_Atoms?.Dispose();
		InstancedIndirectDraw_Atoms = null;
		DispatchIndirect_Simulate_EvaluateForces?.Dispose();
		DispatchIndirect_Simulate_EvaluateForces = null;
		SortedAtomIndexes?.Dispose();
		SortedAtomIndexes = null;
		HashCodeToSortedAtomIndexes?.Dispose();
		HashCodeToSortedAtomIndexes = null;

		foreach (var c in hitResultsPool)
			c.Dispose();
		hitResultsPool.Clear();

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

		GUILayout.Label("hashcode max num " + HashCodeToSortedAtomIndexes_Length);
		if (GUILayout.Button("increase *2"))
		{
			HashCodeToSortedAtomIndexes_Length *= 2;
			OnDestroy();
			Start();
		}
		if (GUILayout.Button("decrease /2"))
		{
			HashCodeToSortedAtomIndexes_Length /= 2;
			OnDestroy();
			Start();
		}

		DrawAtoms = GUILayout.Toggle(DrawAtoms, nameof(DrawAtoms));
		DrawAtomsWithStandartLit = GUILayout.Toggle(DrawAtomsWithStandartLit, nameof(DrawAtomsWithStandartLit));
		AllowPlayerAtomDrag = GUILayout.Toggle(AllowPlayerAtomDrag, nameof(AllowPlayerAtomDrag));
		UseZCurveHashCode = GUILayout.Toggle(UseZCurveHashCode, nameof(UseZCurveHashCode));
		RunBitonicSort = GUILayout.Toggle(RunBitonicSort, nameof(RunBitonicSort));
		UseBitonicSortGroupSharedMemory = GUILayout.Toggle(UseBitonicSortGroupSharedMemory, nameof(UseBitonicSortGroupSharedMemory));
		PreorderData = GUILayout.Toggle(PreorderData, nameof(PreorderData));
		ShouldRunSimulation = GUILayout.Toggle(ShouldRunSimulation, nameof(ShouldRunSimulation));
		ClampAtomsToXyPlane = GUILayout.Toggle(ClampAtomsToXyPlane, nameof(ClampAtomsToXyPlane));
		RunSimulateEvaluateForces = GUILayout.Toggle(RunSimulateEvaluateForces, nameof(RunSimulateEvaluateForces));

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
			//BoundingPlanes_NormalDistance.SetData(boundingPlanes);
		}

		if (RunBitonicSort)
		{
			{
				var bitonicSort = ConfigComputeShader.FindKernel("BitonicSort_Initialize");
				ConfigComputeShader.SetFloat("AtomInteractionMaxRadius", AtomInteractionMaxRadius);
				ConfigComputeShader.SetInt("HashCodeToSortedAtomIndexes_Length", HashCodeToSortedAtomIndexes_Length);
				ConfigComputeShader.SetBool("UseZCurveHashCode", UseZCurveHashCode);
				ConfigComputeShader.SetInt("AllAtoms_Length", AllAtoms_Length);
				ConfigComputeShader.SetBuffer(bitonicSort, "AllAtoms", AllAtoms);
				ConfigComputeShader.SetBuffer(bitonicSort, "SortedAtomIndexes", SortedAtomIndexes);
				ConfigComputeShader.Dispatch(bitonicSort, AllAtoms_Length / GetKernelSizex(bitonicSort), 1, 1);
			}

			{
				for (int DirectionChangeStride = 2; DirectionChangeStride <= AllAtoms_Length; DirectionChangeStride *= 2)
				{
					for (int ComparisonOffset = DirectionChangeStride / 2; true; ComparisonOffset /= 2)
					{
						if (UseBitonicSortGroupSharedMemory && ComparisonOffset < 128)
						{
							var bitonicSort = ConfigComputeShader.FindKernel("BitonicSort_Sort_GroupShared");
							ConfigComputeShader.SetBuffer(bitonicSort, "SortedAtomIndexes", SortedAtomIndexes);
							ConfigComputeShader.SetInt("DirectionChangeStride", DirectionChangeStride);
							ConfigComputeShader.SetInt("ComparisonOffset", ComparisonOffset);
							ConfigComputeShader.Dispatch(bitonicSort, AllAtoms_Length / GetKernelSizex(bitonicSort), 1, 1);
							break;
						}
						else
						{
							var bitonicSort = ConfigComputeShader.FindKernel("BitonicSort_Sort");
							ConfigComputeShader.SetBuffer(bitonicSort, "SortedAtomIndexes", SortedAtomIndexes);
							ConfigComputeShader.SetInt("DirectionChangeStride", DirectionChangeStride);
							ConfigComputeShader.SetInt("ComparisonOffset", ComparisonOffset);
							ConfigComputeShader.Dispatch(bitonicSort, AllAtoms_Length / GetKernelSizex(bitonicSort), 1, 1);
							if (ComparisonOffset == 1) break;
						}
					}
				}
			}

			{
				{
					var hashCodeToSortedAtomIndexes_Initialize = ConfigComputeShader.FindKernel("HashCodeToSortedAtomIndexes_Initialize");
					ConfigComputeShader.SetBuffer(hashCodeToSortedAtomIndexes_Initialize, "HashCodeToSortedAtomIndexes", HashCodeToSortedAtomIndexes);
					ConfigComputeShader.Dispatch(hashCodeToSortedAtomIndexes_Initialize, HashCodeToSortedAtomIndexes_Length / GetKernelSizex(hashCodeToSortedAtomIndexes_Initialize), 1, 1);
				}

				{
					var hashCodeToSortedAtomIndexes_Bin = ConfigComputeShader.FindKernel("HashCodeToSortedAtomIndexes_Bin");
					ConfigComputeShader.SetFloat("AtomInteractionMaxRadius", AtomInteractionMaxRadius);
					ConfigComputeShader.SetInt("AllAtoms_Length", AllAtoms_Length);
					ConfigComputeShader.SetBuffer(hashCodeToSortedAtomIndexes_Bin, "HashCodeToSortedAtomIndexes", HashCodeToSortedAtomIndexes);
					ConfigComputeShader.SetBuffer(hashCodeToSortedAtomIndexes_Bin, "SortedAtomIndexes", SortedAtomIndexes);
					ConfigComputeShader.Dispatch(hashCodeToSortedAtomIndexes_Bin, AllAtoms_Length / GetKernelSizex(hashCodeToSortedAtomIndexes_Bin), 1, 1);
				}
			}

			if (PreorderData)
			{
				var reorderData = ConfigComputeShader.FindKernel("BitonicSort_ReorderData");
				ConfigComputeShader.SetBuffer(reorderData, "SortedAtomIndexes", SortedAtomIndexes);
				ConfigComputeShader.SetBuffer(reorderData, "AllAtoms_Read", AllAtoms);
				ConfigComputeShader.SetBuffer(reorderData, "AllAtoms", AllAtoms2);
				ConfigComputeShader.SetBuffer(reorderData, "AllHalfBonds_Read", AllHalfBonds);
				ConfigComputeShader.SetBuffer(reorderData, "AllHalfBonds", AllHalfBonds2);
				ConfigComputeShader.Dispatch(reorderData, AllAtoms_Length / GetKernelSizex(reorderData), 1, 1);

				Swap(ref AllAtoms, ref AllAtoms2);
				Swap(ref AllHalfBonds, ref AllHalfBonds2);
			}
		}

		// raycast find atom under mouse
		if (AllowPlayerAtomDrag)
		{
			if (!hitResultsPool.TryDequeue(out var hitResults))
				hitResults = new ComputeBuffer(emptyHitResult.Count, Marshal.SizeOf(typeof(int)) * 4, ComputeBufferType.Structured);
			hitResults.SetData(emptyHitResult);

			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			var raycastHitAtoms = ConfigComputeShader.FindKernel("RaycastHitAtoms");
			ConfigComputeShader.SetFloat("AtomRadius", atomRadius);
			ConfigComputeShader.SetVector("RayStartWorldPos", ray.origin);
			ConfigComputeShader.SetVector("RayDirection", ray.direction);
			ConfigComputeShader.SetInt("AllAtoms_Length", AllAtoms_Length);
			ConfigComputeShader.SetBuffer(raycastHitAtoms, "AllAtoms", AllAtoms);
			ConfigComputeShader.SetBuffer(raycastHitAtoms, "HitResults", hitResults);
			ConfigComputeShader.Dispatch(raycastHitAtoms, AllAtoms_Length / GetKernelSizex(raycastHitAtoms), 1, 1);

			AsyncGPUReadback.Request(hitResults, (result) =>
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

				hitResultsPool.Enqueue(hitResults);
			});
		}

		// drag atom
		if (AllowPlayerAtomDrag)
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
				ConfigComputeShader.SetInt("AllAtoms_Length", AllAtoms_Length);
				ConfigComputeShader.SetBuffer(simulate, "AllAtoms", AllAtoms);
				ConfigComputeShader.SetInt("AllHalfBonds_Length", AllHalfBonds_Length);
				ConfigComputeShader.SetBuffer(simulate, "AllHalfBonds", AllHalfBonds);
				ConfigComputeShader.SetInt("BoundingPlanes_Length", BoundingPlanes_Length);
				ConfigComputeShader.SetBuffer(simulate, "BoundingPlanes_NormalDistance", BoundingPlanes_NormalDistance);
				ConfigComputeShader.SetFloat("AtomInteractionMaxRadius", AtomInteractionMaxRadius);
				ConfigComputeShader.SetBool("ClampTo2D", ClampAtomsToXyPlane);
				ConfigComputeShader.Dispatch(simulate, AllAtoms_Length / GetKernelSizex(simulate), 1, 1);
			}

			if (RunSimulateEvaluateForces)
			{
				var simulate = ConfigComputeShader.FindKernel("Simulate_EvaluateForces");
				ConfigComputeShader.SetBool("DataAlreadyOrdered", PreorderData);
				ConfigComputeShader.SetFloat("AtomRadius", atomRadius);
				ConfigComputeShader.SetInt("AllAtoms_Length", AllAtoms_Length);
				ConfigComputeShader.SetBuffer(simulate, "AllAtoms", AllAtoms);
				ConfigComputeShader.SetInt("AllHalfBonds_Length", AllHalfBonds_Length);
				ConfigComputeShader.SetBuffer(simulate, "AllHalfBonds", AllHalfBonds);
				ConfigComputeShader.SetInt("BoundingPlanes_Length", BoundingPlanes_Length);
				ConfigComputeShader.SetBuffer(simulate, "BoundingPlanes_NormalDistance", BoundingPlanes_NormalDistance);
				ConfigComputeShader.SetFloat("AtomInteractionMaxRadius", AtomInteractionMaxRadius);
				ConfigComputeShader.SetBuffer(simulate, "HashCodeToSortedAtomIndexes", HashCodeToSortedAtomIndexes);
				ConfigComputeShader.SetInt("HashCodeToSortedAtomIndexes_Length", HashCodeToSortedAtomIndexes_Length);
				ConfigComputeShader.SetBool("UseZCurveHashCode", UseZCurveHashCode);
				ConfigComputeShader.SetBuffer(simulate, "SortedAtomIndexes", SortedAtomIndexes);
				ConfigComputeShader.SetBool("ClampTo2D", ClampAtomsToXyPlane);
				ConfigComputeShader.DispatchIndirect(simulate, DispatchIndirect_Simulate_EvaluateForces);
			}

			{
				var simulate = ConfigComputeShader.FindKernel("Simulate_Move");
				ConfigComputeShader.SetFloat("DeltaTime", 0.05f);
				ConfigComputeShader.SetInt("AllAtoms_Length", AllAtoms_Length);
				ConfigComputeShader.SetBuffer(simulate, "AllAtoms", AllAtoms);
				ConfigComputeShader.SetInt("AllHalfBonds_Length", AllHalfBonds_Length);
				ConfigComputeShader.SetBuffer(simulate, "AllHalfBonds", AllHalfBonds);
				ConfigComputeShader.SetFloat("AtomInteractionMaxRadius", AtomInteractionMaxRadius);
				ConfigComputeShader.SetBool("ClampTo2D", ClampAtomsToXyPlane);
				ConfigComputeShader.Dispatch(simulate, AllAtoms_Length / GetKernelSizex(simulate), 1, 1);
			}
		}

		if (DrawAtoms)
			DrawAtomsInstanced();
	}

	public static void Swap<T>(ref T lhs, ref T rhs)
	{
		T temp = lhs;
		lhs = rhs;
		rhs = temp;
	}

	ushort GetKernelSizex(string name)
	{
		return GetKernelSizex(ConfigComputeShader.FindKernel(name));
	}
	ushort GetKernelSizex(int id)
	{
		ConfigComputeShader.GetKernelThreadGroupSizes(id, out uint x, out uint y, out uint z);
		return (ushort)x;
	}

	void SetDrawMaterialParams(Material material)
	{
		material.SetInt("AllAtoms_Length", AllAtoms_Length);
		material.SetBuffer("AllAtoms", AllAtoms);
		material.SetInt("AllHalfBonds_Length", AllHalfBonds_Length);
		material.SetBuffer("AllHalfBonds", AllHalfBonds);
		material.SetBuffer("HashCodeToSortedAtomIndexes", HashCodeToSortedAtomIndexes);
		material.SetInt("HashCodeToSortedAtomIndexes_Length", HashCodeToSortedAtomIndexes_Length);
		material.SetInt("UseZCurveHashCode", UseZCurveHashCode ? 1 : 0);
		material.SetFloat("AtomRadius", atomRadius);
		material.SetFloat("AtomInteractionMaxRadius", AtomInteractionMaxRadius);
	}

	void DrawAtomsInstanced()
	{
		var bounds = new Bounds(Vector3.zero, Vector3.one * 1000);
		if (DrawAtomsWithStandartLit)
		{
			SetDrawMaterialParams(ConfigAtomsMaterialStandartLit);
			Graphics.DrawMeshInstancedIndirect(ConfigAtomMesh, 0, ConfigAtomsMaterialStandartLit, bounds, InstancedIndirectDraw_Atoms, 0, null, ShadowCastingMode.On, true, 0, null, LightProbeUsage.BlendProbes);
			SetDrawMaterialParams(ConfigHalfBondMaterialStandartLit);
			Graphics.DrawMeshInstancedIndirect(ConfigHalfBondMesh, 0, ConfigHalfBondMaterialStandartLit, bounds, InstancedIndirectDraw_HalfBonds, 0, null, ShadowCastingMode.On, true, 0, null, LightProbeUsage.BlendProbes);
		}
		else
		{
			SetDrawMaterialParams(ConfigAtomsMaterialSimple);
			Graphics.DrawMeshInstancedIndirect(ConfigAtomMesh, 0, ConfigAtomsMaterialSimple, bounds, InstancedIndirectDraw_Atoms, 0, null, ShadowCastingMode.Off, false, 0, null, LightProbeUsage.Off);
			SetDrawMaterialParams(ConfigHalfBondMaterialSimple);
			Graphics.DrawMeshInstancedIndirect(ConfigHalfBondMesh, 0, ConfigHalfBondMaterialSimple, bounds, InstancedIndirectDraw_HalfBonds, 0, null, ShadowCastingMode.Off, false, 0, null, LightProbeUsage.Off);
		}
	}

	void ReadbackAtomPosition(uint atomIndex, System.Action<Vector3> resultCallback)
	{
		AsyncGPUReadback.Request(AllAtoms, AllAtoms.stride, AllAtoms.stride * (int)atomIndex, (result) =>
		{
			var data = result.GetData<Atom>();
			resultCallback?.Invoke(data[0].position);
		});
	}
}

