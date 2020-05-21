using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Providers.Grid;
using UnityEngine;
using Vella.SimpleBurstCollision;
using Vector3 = UnityEngine.Vector3;

[ExecuteInEditMode]
public class GridVolumeModifier : MonoBehaviour
{
    //public NodeFlagSettingGroup NodeFlagsSettings;
    //public bool ShowNodeFlags;

    [Header("Setup")]
    public GridManager GridManager;
    public Collider Collider;
    public NodeFlags AddFlags;
    public NodeFlags RemoveFlags;
    public NodeFlags Restriction;

    [Header("Debug Options")]
    public bool ShowPointsOnCollider;
    public bool ShowScannedNodes;
    public bool ShowClosestNode;
    public bool LogPerformance;

    public bool ExecuteInEditMode;

    private BurstBoxCollider Box;

    void Start()
    {
        if (Collider == null)
        {
            Collider = GetComponent<Collider>();
        }
    }

    private GridNode _closestNodeToCenter;
    private List<GridNode> _touchedNodes = new List<GridNode>();
    private readonly List<Vector3> _colliderPoints = new List<Vector3>();
    private List<GridNode> _nodesInRange = new List<GridNode>();
    private Stopwatch _stopwatch = new Stopwatch();
    private int[] _trackedIndices;

    void Update()
    {
        if (!ExecuteInEditMode && !Application.isPlaying)
            return;

        if (GridManager == null || !GridManager.IsValid)
            return;

        if (!isActiveAndEnabled || !transform.hasChanged)
            return;

        Box = BurstColliderFactory.CreateBox(transform);

        if (LogPerformance)
        {
            _stopwatch.Restart();
        }

        var box = BurstColliderFactory.CreateBox(Collider as BoxCollider);

        _closestNodeToCenter = GridManager.Grid.FindClosestNode(Box.Center);

        _nodesInRange.Clear();
        _touchedNodes.Clear();

        if (GridManager.Grid.CollidesWithNode(Collider, _closestNodeToCenter, out float gapDistance, out Vector3 pointOnCollider))
        {
            //var scanDistance = 1 + (int) Math.Round((Collider.bounds.size.x / 2f) - gapDistance, MidpointRounding.AwayFromZero);

            //var boxWorldMin = Box.Center - Box.Min;
            //var boxWorldMax = Box.Center - Box.Max;

            //var a = math.transform(Box.ToWorldMatrix, Box.Min);
            //var b = math.transform(Box.ToWorldMatrix, Box.Max);

            //DebugExtension.DebugWireSphere(a, Color.yellow, 0.1f);
            //DebugExtension.DebugWireSphere(b, Color.yellow, 0.1f);

            //var minNode = GridManager.Grid.FindClosestNode(Box.Bounds.min);
            //var maxNode = GridManager.Grid.FindClosestNode(Box.Bounds.max);

            //var debug1 = GridManager.Grid.ToWorldPosition(minNode.Center);
            //var debug2 = GridManager.Grid.ToWorldPosition(maxNode.Center);

            //DebugExtension.DebugWireSphere(debug1, Color.red, 0.1f);
            //DebugExtension.DebugWireSphere(debug2, Color.red, 0.1f);

            //var sw = Stopwatch.StartNew();

            _trackedIndices = GridNodeJobs.AllNodes.IntersectionDiff.SetFlagsInBoxDiff.Run(GridManager.Grid, (ulong)AddFlags, box, _trackedIndices);

            //sw.Stop();
            //Debug.Log($"Took: {sw.Elapsed.TotalMilliseconds:N8} ms");
        }
    }

    void OnDisable()
    {
        if (GridManager == null)
            return;

        RestoreNodes(_touchedNodes);
    }

    private void RestoreNodes(IList<GridNode> previousSet, IList<GridNode> newSet = null)
    {
        var orphans = newSet != null
            ? previousSet.Except(newSet, GridPointComparer.Instance)
            : previousSet;

        foreach (var node in orphans.ToList())
        {
            node.AddFlags(NodeFlags.AllowWalk);
            previousSet.Remove(node);
        }
    }

    public class GridPointComparer : IEqualityComparer<GridNode>
    {
        public static GridPointComparer Instance { get; } = new GridPointComparer();

        public bool Equals(GridNode x, GridNode y)
        {
            return x.GridPoint == y.GridPoint;
        }

        public int GetHashCode(GridNode obj)
        {
            unchecked
            {
                return obj.GridPoint.X.GetHashCode() + obj.GridPoint.Y.GetHashCode() + obj.GridPoint.Z.GetHashCode();
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!ExecuteInEditMode && !Application.isPlaying)
            return;

        DrawingHelpers.DrawWireFrame(Box);
    }

//    void OnEnable()
//    {
//#if UNITY_EDITOR
//        EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
//#endif
//    }

//#if UNITY_EDITOR
//    private void EditorApplication_playModeStateChanged(PlayModeStateChange state)
//    {
//        switch (state)
//        {
//            case PlayModeStateChange.ExitingEditMode:
//            case PlayModeStateChange.ExitingPlayMode:
//                EnsureDestroyed();
//                break;
//        }
//    }
//#endif

//    void OnDisable()
//    {
//#if UNITY_EDITOR
//        EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;
//#endif
//        GridManager = null;
//    }


}

