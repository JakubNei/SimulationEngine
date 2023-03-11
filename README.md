
![](.images/Screenshot-2023-02-18.png)


# System requirements
Unity 2021.3.18f1 LTS

# How does it work

-	Bitonic sort particle index array by hashcode of their position. Hashcode represents index of voxel cell where particle is. 
-	Build array that gives us index+count (in sorted particle index array) from hashcode.
-	Simulate each particle.
	-	Fetch particles from neighbouring 27 voxel cells using hashcode.
	-	Calculate forces from those particles.
	-	Update position and velocity.
-	Instanced draw all particles from positions buffer.

Each particle can collide with all other particles, but we leverage the limited interaction radius of particles (the voxel cell edge length corresponds to the maximum interaction radius). So after bitonic sort and hascode, the complexity is only $O(nlog_2(n))$ instead of $O(n^2)$

# Todo


## Optimizations
- Fetch from cells only relevant for particle octant
- [Leverage group shared memory](https://github.com/hiroakioishi/UnityGPUBitonicSort/blob/master/GPUBitonicSort/Assets/BitonicSortCS/BitonicSort.compute#L56)

# Resources
[A More Efficient Parallel Method For Neighbour Search Using CUDA](http://diglib.eg.org/bitstream/handle/10.2312/vriphys20151339/101-109.pdf?fbclid=IwAR26EUM2MlLdBVF2R-NkF0bjqqJYFX8tfkGLBqNXHNTqLG3fWdj0-wn-FoU)
[Bitonic sort Wikipedia](https://en.wikipedia.org/wiki/Bitonic_sorter)
[Bitonic sort example Unity compute shader](https://github.com/hiroakioishi/UnityGPUBitonicSort/blob/master/GPUBitonicSort/Assets/BitonicSortCS/BitonicSort.compute)