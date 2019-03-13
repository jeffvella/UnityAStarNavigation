# UnityAStarNavigation

An experimental 3D A* navigation system for Unity using burst compiled jobs.

##### Features:

* 3D A* Pathfinding queries.
* Custom Native 3D dense grid ontop of a single NativeArray<T>
* Each query can specify its own area weights.
* Grid nodes can be assigned to many areas.
* Grid nodes can be added/removed from areas by volume oriented bounding boxes.
* Grid nodes can be generated for terrain by mapping to a Unity NavMesh.
* Custom NativePriorityQueue<T>

<img src="https://i.imgur.com/bDRp9Jv.gif" target="_blank" />

Note: Project was created with Unity 2019.2, older versions may not work.
