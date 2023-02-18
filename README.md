
![](.images/Screenshot-2023-02-18.png)


# Unity
uses Unity version 2021.3.18f1 LTS

# How does it work

-	Bitonic sort particle index array by hashcode of their position
	Hashcode represents index of voxel cell where particle is
-	Build array that gives us index+count (in sorted particle index array) from hashcode
-	Simulate each particle
	-	Fetch particles from neighbouring 27 voxel cells using hashcode
	-	Calculate forces from those particles
	-	Update position and velocity
-	Instanced draw all particles from positions buffer

# Resources
A More Efficient Parallel Method For Neighbour Search Using CUDA
http://diglib.eg.org/bitstream/handle/10.2312/vriphys20151339/101-109.pdf?fbclid=IwAR26EUM2MlLdBVF2R-NkF0bjqqJYFX8tfkGLBqNXHNTqLG3fWdj0-wn-FoU