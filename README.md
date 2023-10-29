
![](.images/Screenshot-2023-03-28.png)
![](.images/Screenshot-2023-02-18.png)

Discontinued. My attempt at PhD with collaboration with one researcher friend. Ended due to not having time and motivation due to work. Learned things that became useful later.

# System requirements
Unity 2021.3.18f1 LTS

# Current state, how it works
- [Bitonic sort][Bitonic sort Wikipedia] atom index array by hashcode of their position. Hashcode represents index of voxel cell where atom is. 
- Build array that we can use to find a pair of `[index start, index end]` (to array built in above step) from voxel cell hashcode. [We can build this array by comparing hashcodes of pairs of atoms and if they are different we found start and end][NVIDIA Particle Simulation using CUDA, 2010].
- Update each atom force using [Prokop Hapala's RARFF Solver]
	- Fetch atoms from neighbouring 27 voxel cells using hashcode.
	- Calculate forces.
- Update each atom velocity and position.
- Instanced draw everything.
	- All atoms
	- All half bonds

Each atom can collide with all other atoms, but we leverage the limited interaction radius of atoms (the voxel cell edge length corresponds to the maximum interaction radius). So after bitonic sort and hascode, the complexity is only $O(nlog_2(n))$ instead of $O(n^2)$

# Relevant most useful algorithms/concepts
## Building the Grid using Sorting 
[NVIDIA Particle Simulation using CUDA, 2010]
> An alternative approach which does not require atomic operations is to use sorting. The algorithm consists of several kernels. The first kernel “calcHash” calculates a hash value for each particle based on its cell id. In this example we simply use the linear cell id as the hash, but it may be beneficial to use other functions such the Z-order curve [8] to improve the coherence of memory accesses. The kernel stores the results to the “particleHash” array in global memory as a uint2 pair (cell hash, particle id). We then sort the particles based on their hash values. The sorting is performed using the fast radix sort provided by the CUDPP library, which uses the algorithm described in [12]. This creates a list of particle ids in cell order. In order for this sorted list to useful, we need to be able to find the start of any given cell in the sorted list. This is achieved by running another kernel “findCellStart”, which uses a thread per particle and compares the cell index of the current particle with the cell index of the previous particle in the sorted list. If the index is different, this indicates the start of a new cell, and the start address is written to another array using a scattered write. The current code also finds the index of the end of each cell in a similar way.

## Bitonic sort
Bitonic sort has $O(nlog_2(n))$ complexity in all cases, it does not leverage cases where array is already close to sorted order, that is why radix sort might have better performance. Bitonic sort is however very simple to implement:
```hlsl
void BitonicSort(uint3 id : SV_DispatchThreadID)
{
    int index0 = id.x & ~ComparisonOffset; // index top
	int index1 = id.x | ComparisonOffset; // index bottom
    if (index1 == id.x) return;    
    uint hashcode0 = SortedAtomIndexes[index0].y;
    uint hashcode1 = SortedAtomIndexes[index1].y;
    bool shouldSwap = (hashcode0 <= hashcode1) == (bool)(DirectionChangeStride & index0);
    if (shouldSwap)
    {
        uint2 temp = SortedAtomIndexes[index0];
        SortedAtomIndexes[index0] = SortedAtomIndexes[index1];
        SortedAtomIndexes[index1] = temp;
    }
}
```

## Prokop Hapala's RARFF


# What to try next
## Electron Force Field
[An Electron Force Field for Simulating Large Scale Excited Electron Dynamics, 2007]
> zkusit implementovat electron-forcefield (eFF) ... ten je uz trochu kvantovy ... a nikdo ho na GPU nema, bylo by to dost cool jako nejaka edukativni hra... ale tam je trochu komplikace:
> 1) ty elektrony se nafukuji (jsou to jakoby natlakovane oblacky/balonky, cim jsou vetsi tim maji mensi energii)
> 2) jsou tam silne daleko-dosahove interakce (elekstrostatika), takze ta akcelerace kratko-dosahovych interaci (=kolizi) az tolik nepomuze

Can optimize using [Fast multipole method](https://en.wikipedia.org/wiki/Fast_multipole_method)

# Optimizations

## What helped
- Major
  - For N x N body simulation
      - Limitng max interaction radius, which allows us to sort particle by cell hashcode, where cell size is max interaction radius, then we can check interactions only with neighbouring 27 cells [NVIDIA Particle Simulation using CUDA, 2010]
      - Increasing max hashcode, which decreased hashcode collisions
      - Distributing neighbouring 27 cells calculations into individual compute shader threads, 36-39ms vs 27ms, higher number of simpler kernels seems better
      - Decreasing hashcode collisions by increasing possible hashcode space
  - Switching to Vulkan from OpenGLCore on Linux increased Compute Shader performance
- Noticable
    - In Bitonic Sort using additional kernel that uses shared memory when we compare keys with small offset [Bitonic sort example Unity compute shader]

## What didnt help
- Ordering actual data (not just indexes) according to hashcode didn't help. There was no performance improvemenet in interaction forces evaluation kernel, even thought in theory it should improve coherence of memory. Only additional cost of the reordering kernel. [NVIDIA Particle Simulation using CUDA, 2010] Maybe poorly implemented ? There seems to be some issue with it.
- Hashing with Z Curve did not help maybe poorly implemented ?
## To try
- Use two buffers, read, write, then swap, instead of using same one
- Can try to use force right don't really need to save it
- Come up with others ways to minimize global memory read and writes (packing data ?)
- Use Z Order curve for hashcode
- Use sorting algorithm that has better preformance with almost sorted array. Atoms are likely to stay in same cell over time.
- Use struct of arrays instead array of structs https://en.wikipedia.org/wiki/AoS_and_SoA https://softwareengineering.stackexchange.com/questions/246474/
- Try data aligment, float3 might be more cache friendly than float4
- First load data into local variables then use those instead of using the global array, might give more time to read data so shader does not wait for memory
- Auto tuning
  - Find best block size
https://forums.developer.nvidia.com/t/how-to-choose-how-many-threads-blocks-to-have/55529/6
  - Find best hashcode implementation
  - Find best hash code to arom index array size (probably bigger is better to reduce hashcode collisions) 
- [Consider using OpenCL in Unity][Using OpenCL in Unity]


# Resources

## Papers

[NVIDIA Particle Simulation using CUDA, 2010]

[NVIDIA Particle Simulation using CUDA, 2010]: https://developer.download.nvidia.com/assets/cuda/files/particles.pdf

[Fast 4-way parallel radix sorting on GPUs, 2009]

[Fast 4-way parallel radix sorting on GPUs, 2009]:https://vgc.poly.edu/~csilva/papers/cgf.pdf

[An Electron Force Field for Simulating Large Scale Excited Electron Dynamics, 2007]

[An Electron Force Field for Simulating Large Scale Excited Electron Dynamics, 2007]:https://thesis.library.caltech.edu/1598/?fbclid=IwAR2ZoADYZzUbqnOLgEGWrlHrHGmFl805R1VBTvMnfogSYXCDGaHpTaE4fDY

[Realtime Fluid Simulation on the GPU with Unity3D]

[Realtime Fluid Simulation on the GPU with Unity3D]:https://pats.cs.cf.ac.uk/@archive_file?p=1680&n=final&f=1-report.pdf&SIG=fa08d62b19872176c2660cc5a71a96849e13dd3e1fb3ecbb7561aab23228ee74

[Efficient Spatial Binning on the GPU, AMD Technical Report, 2009]

[Efficient Spatial Binning on the GPU, AMD Technical Report, 2009]:https://www.chrisoat.com/papers/EfficientSpatialBinning.pdf

[A More Efficient Parallel Method For Neighbour Search Using CUDA, 2015]

[A More Efficient Parallel Method For Neighbour Search Using CUDA, 2015]:http://diglib.eg.org/bitstream/handle/10.2312/vriphys20151339/101-109.pdf?fbclid=IwAR26EUM2MlLdBVF2R-NkF0bjqqJYFX8tfkGLBqNXHNTqLG3fWdj0-wn-FoU

## Wikipedia

[Bitonic sort Wikipedia]

[Bitonic sort Wikipedia]:https://en.wikipedia.org/wiki/Bitonic_sorter

## Example/reference code

[Bitonic sort example Unity compute shader]

[Bitonic sort example Unity compute shader]:https://github.com/hiroakioishi/UnityGPUBitonicSort/blob/master/GPUBitonicSort/Assets/BitonicSortCS/BitonicSort.compute

[Using OpenCL in Unity]

[Using OpenCL in Unity]:https://forum.unity.com/threads/opencl-from-unity.720719/

[Prokop Hapala's RARFF Solver]

[Prokop Hapala's RARFF Solver]:https://github.com/ProkopHapala/SimpleSimulationEngine/blob/master/cpp/common/molecular/RARFF_SR.h

[Prokop Hapala's RARFF Solver Test]

[Prokop Hapala's RARFF Solver Test]:https://github.com/ProkopHapala/SimpleSimulationEngine/blob/master/cpp/sketches_SDL/Molecular/test_RARFF_SR.cpp

[GPU Radix Sort]

[GPU Radix Sort]:https://github.com/mark-poscablo/gpu-radix-sort

## Other

[18 - How to write a FLIP water / fluid simulation running in your browser]

[18 - How to write a FLIP water / fluid simulation running in your browser]:https://www.youtube.com/watch?v=XmzBREkK8kY