using System;
using System.Collections.Generic;
using System.Linq;
using Providers.Grid;
using Vella.Common.Navigation;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR

#endif

[ExecuteInEditMode]
public class PathTester : MonoBehaviour
{
    public GridManager GridManager;

    public Transform StartTransform;
    public Transform EndTransform;
    public bool AutoUpdate;

    public LineRenderer LineRenderer;

    private DateTime _lastUpdateTime = DateTime.MinValue;

    private GridNode _closestNode;
    private GridNode _exitNode;

    public Color LineColor = Color.white;

    public NodeFlags AllowedFlags = NodeFlags.AllowWalk;

    public BurstAStarPathFinder PathFinder;

    public float PathOffset = 0.1f;
    public bool ShowGizmoPath = true;
    public bool HasCreatedFirstPath;

    internal List<float> QueryTimeHistory = new List<float>();

    public float AverageQueryTime => QueryTimeHistory.Any() ? QueryTimeHistory.Average() : 0;

    void Awake()
    {

    }

    private void Updated()
    {
        Debug.Log("Grid Updated, Updating Path "+ name);
        UpdatePath();
    }

    void OnEnable()
    {
        _startRenderer = StartTransform.GetComponent<Renderer>();
        _endRenderer = EndTransform.GetComponent<Renderer>();

        _mat = new Material(Shader.Find("Specular"));
        _startRenderer.sharedMaterial = _mat;
        _endRenderer.sharedMaterial = _mat;

        Areas = new AreaDefinitions<NodeFlags>
        {
            {NodeFlags.Combat, 0.4f},
            {NodeFlags.Avoidance, -2f},
            {NodeFlags.NearEdge, -0.5f}
        };

        UpdatePath();
    }

    public AreaDefinitions<NodeFlags> Areas { get; set; }


    void Start()
    {

    }

    private Vector3 _lastStartTransformPosition;
    private Vector3 _lastEndTransformPosition;
    private Color _lastLineColor;

    private IEnumerable<GridNode> _cleanedPath;
    private Renderer _startRenderer;
    private Renderer _endRenderer;
    private Material _mat;

    void Update()
    {
        if (GridManager == null || !GridManager.IsValid)
            return;

        if (HasCreatedFirstPath && !GridManager.transform.hasChanged && DateTime.UtcNow.Subtract(_lastUpdateTime).TotalMilliseconds < 25)
            return;

        if (!HasCreatedFirstPath || AutoUpdate && (StartTransform.position != _lastStartTransformPosition || EndTransform.position != _lastEndTransformPosition))
        {
            UpdatePath();

            if (PathFinder.Path.Count > 0)
            {
                HasCreatedFirstPath = true;
            }
        }

        _lastLineColor = LineColor;
        _lastEndTransformPosition = EndTransform?.position ?? Vector3.zero;
        _lastStartTransformPosition = StartTransform?.position ?? Vector3.zero;
        _lastUpdateTime = DateTime.UtcNow;
    }

    public void UpdatePath()
    {
        _closestNode = default;
        _exitNode = default;

        if (_startRenderer != null)
        {
            _startRenderer.sharedMaterial.color = LineColor;
        }

        if (_endRenderer != null)
        {
            _endRenderer.sharedMaterial.color = LineColor;
        }

        if (GridManager != null && GridManager.Grid != null)
        {
            if (PathFinder == null)
            {
                PathFinder = new BurstAStarPathFinder(GridManager.Grid)
                {
                    Areas = Areas,
                    Grid = GridManager.Grid,
                };
            }

            _closestNode = GridManager.Grid.FindClosestNode(StartTransform.position, AllowedFlags, 20);
            if (_closestNode.Equals(default))
            {
                Debug.Log("Closest Node Not Found");
                return;
            }

            _exitNode = GridManager.Grid.FindClosestNode(EndTransform.position, AllowedFlags, 20);
            if (_exitNode.Equals(default))
            {
                return;
            }

            if (_closestNode.Equals(default) || _exitNode.Equals(default))
            {
                Debug.Log("Nodes not found");
            }
            else
            {
                GetPath();

                if (LineRenderer != null)
                {
                    _cleanedPath = MergeLikeDirectionPathSegments(PathFinder.NodePath);
                    var path = OffsetPath(_cleanedPath, transform.position, PathOffset).ToList();
                    path.Insert(0, StartTransform.position);
                    path.Add(EndTransform.position);
                    LineRenderer.positionCount = path.Count;
                    LineRenderer.SetPositions(path.ToArray());

                    if (LineRenderer.sharedMaterial == null)
                    {
                        LineRenderer.sharedMaterial = _mat;
                    }
                    else
                    {
                        LineRenderer.sharedMaterial.color = LineColor;
                    }
                }
            }

#if UNITY_EDITOR
            SceneView.RepaintAll();
#endif
        }
    }

    public IEnumerable<Vector3> OffsetPath(IEnumerable<GridNode> nodes, Vector3 origin, float amount)
    {
        foreach (var node in nodes)
        {
            yield return GridManager.Grid.Transform.GetWorldPosition(node.NavigableCenter) + node.Normal * amount;
        }
    }

    public IEnumerable<GridNode> MergeLikeDirectionPathSegments(IEnumerable<GridNode> nodes)
    {
        GridNode last = default;
        Vector3 lastDir = Vector3.zero;
        foreach (var node in nodes)
        {
            if (last == null)
            {
                last = node;
                yield return node;
                continue;
            }
            var dir = node.NavigableCenter - last.NavigableCenter;
            if (dir != lastDir)
            {
                yield return node;
            }
            lastDir = dir;
        }
    }

    public void GetPath()
    {
        if (GridManager == null)
            return;

        if (PathFinder.Grid.InnerGrid.IsDisposed)
        {
            PathFinder.Grid = GridManager.Grid;
        }

        PathFinder.Clear();
        PathFinder.AllowFlags = (ulong)AllowedFlags;
        PathFinder.GetPath(_closestNode, _exitNode);


    }

    public void ClearPath()
    {
        PathFinder?.NodePath?.Clear();
    }

    void OnDrawGizmos()
    {
        if (!_closestNode.Equals(default) && !_exitNode.Equals(default))
        {
            if (PathFinder.NodePath != null && PathFinder.NodePath.Count > 0)
            {
                if (ShowGizmoPath)
                {
                    var firstNode = PathFinder.NodePath.First();
                    var firstPathPosition = GetWorldPosition(firstNode);
                    var lastNode = PathFinder.NodePath.Last();
                    var lastPathPosition = GetWorldPosition(lastNode);

                    Gizmos.color = LineColor;
                    Gizmos.DrawSphere(firstPathPosition + Vector3.up * PathOffset, 0.1f);

                    var offset = Vector3.up * PathOffset;
                    var from = firstPathPosition;
                    foreach (var node in PathFinder.NodePath.Skip(1))
                    {
                        var to = GetWorldPosition(node);
                        Gizmos.DrawLine(from + offset, to + offset);
                        Gizmos.DrawSphere(to + offset, 0.1f);
                        from = to;
                    }

                    Gizmos.DrawLine(StartTransform.position, firstPathPosition + offset);
                    Gizmos.DrawLine(EndTransform.position, lastPathPosition + offset);
                }
            }
        }

    }

    private Vector3 GetWorldPosition(GridNode firstNode)
    {
        return GridManager.Grid.Transform.GetWorldPosition(firstNode.NavigableCenter) + firstNode.Normal * PathOffset;
    }
}


#if UNITY_EDITOR

[CustomEditor(typeof(PathTester))]
[CanEditMultipleObjects]
public class PathTester_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        foreach (var targ in targets.Cast<PathTester>())
        {
            //if (!targ.AutoUpdate)
            //{
                if (GUILayout.Button("Generate Path"))
                {
                    targ.UpdatePath();
                }
                if (GUILayout.Button("Clear Path"))
                {
                    targ.ClearPath();
                    SceneView.RepaintAll();
                }

            //}

            if (targ.PathFinder != null)
            {
                GUILayout.Label($"Last PathFinder Time: {targ.PathFinder.Stopwatch.Elapsed.TotalMilliseconds:N2} ms");
                //GUILayout.Label($"Path Length: {targ.PathFinder.CalculatePathLength()}");
            }
        }
    }
}

#endif