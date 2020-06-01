using System.Collections.Generic;
using System.Linq;
using Providers.Grid;
using UnityEngine;
using Vella.Common.Navigation;
using Random = UnityEngine.Random;

public class PathActor : MonoBehaviour
{
    private GridManager _gridManager;
    private LineRenderer _lineRenderer;
    private const float PathUpdatePeriod = 1f;
    private const float StepUpdatePeriod = 0.1f;
    private float _pathElapsedTime;
    private float _stepElapsedTime;

    public float pathOffset = 0.1f;
    public float speed = 10;

    public bool showGizmoPath = true;
    public Color lineColor = Color.white;

    public NodeFlags allowedFlags = NodeFlags.AllowWalk;
    public AreaDefinitions<NodeFlags> Areas { get; set; }

    internal List<float> QueryTimeHistory = new List<float>();
    public float AverageQueryTime => QueryTimeHistory.Any() ? QueryTimeHistory.Average() : 0;

    public BurstAStarPathFinder pathFinder;
    private GridNode _closestNode;
    private GridNode _exitNode;
    private Material _mat;

    public Vector3 destination;
    private IEnumerable<GridNode> _cleanedPath;

    public int areaSize = 20;

    public Queue<Vector3> nextPositions;
    public Vector3? nextStep;
    private void Awake()
    {
        nextPositions = new Queue<Vector3>();
        _gridManager = GameObject.Find("GridManager").GetComponent<GridManager>();
        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _mat = new Material(Shader.Find("Diffuse"));
        Areas = new AreaDefinitions<NodeFlags>
        {
            {NodeFlags.Combat, 0.4f},
            {NodeFlags.Avoidance, -2f},
            {NodeFlags.NearEdge, -0.5f}
        };

        pathFinder = new BurstAStarPathFinder(_gridManager.Grid)
        {
            Areas = Areas,
            Grid = _gridManager.Grid,
        };
    }

    void Start()
    {
        lineColor = GetRandomColor();
        _mat.SetColor("Color", lineColor);

        destination = GetRandomVector3Xz(areaSize);
        UpdatePath();
    }

    private void Update()
    {
        if (_gridManager == null || !_gridManager.IsValid)
            return;

        if (_pathElapsedTime > PathUpdatePeriod)
        {
            UpdatePath();
            _pathElapsedTime = 0;
            if (nextPositions.Count > 0) nextStep = nextPositions.Dequeue();
            if (nextPositions.Count > 0) nextStep = nextPositions.Dequeue();
        }
        else if (_stepElapsedTime > StepUpdatePeriod)
        {
            if (nextPositions.Count > 0)
            {
                nextStep = nextPositions.Dequeue();
            }
            else
            {
                _pathElapsedTime = 0;
                nextStep = null;
                destination = GetRandomVector3Xz(areaSize);
                UpdatePath();
            }
            _stepElapsedTime = 0;
        }


        _pathElapsedTime += Time.deltaTime;
        _stepElapsedTime += Time.deltaTime;

        if (nextStep == null) return;

        transform.position = Vector3.Lerp(transform.position, nextStep.Value, speed * Time.deltaTime);

        if(Vector3.Distance(transform.position, nextStep.Value) < .1f) nextStep = null;

    }

    public void GetPath()
    {
        if (_gridManager == null)
            return;

        if (pathFinder.Grid.InnerGrid.IsDisposed)
        {
            pathFinder.Grid = _gridManager.Grid;
        }

        pathFinder.Clear();
        pathFinder.AllowFlags = (ulong)allowedFlags;
        pathFinder = pathFinder.GetPath(_closestNode, _exitNode);
        nextPositions.Clear();
        foreach (var pos in pathFinder.Path)
        {
            nextPositions.Enqueue(pos);
        }
    }

    public void UpdatePath()
    {
        //Debug.Log("UpdatePath");
        _closestNode = default;
        _exitNode = default;

        if (_gridManager != null && _gridManager.Grid != null)
        {
            if (pathFinder == null)
            {
                pathFinder = new BurstAStarPathFinder(_gridManager.Grid)
                {
                    Areas = Areas,
                    Grid = _gridManager.Grid,
                };
            }

            _closestNode = _gridManager.Grid.FindClosestNode(transform.position, allowedFlags, areaSize);
            if (_closestNode.Equals(default))
            {
                //Debug.Log("Closest Node Not Found");
                return;
            }

            _exitNode = _gridManager.Grid.FindClosestNode(destination, allowedFlags, areaSize);
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

                if (_lineRenderer != null)
                {
                    _cleanedPath = MergeLikeDirectionPathSegments(pathFinder.NodePath);
                    var path = OffsetPath(_cleanedPath, transform.position, pathOffset).ToList();
                    path.Insert(0, transform.position);
                    path.Add(destination);
                    _lineRenderer.positionCount = path.Count;
                    _lineRenderer.SetPositions(path.ToArray());

                    if (_lineRenderer.sharedMaterial == null)
                    {
                        _lineRenderer.sharedMaterial = _mat;
                    }
                    else
                    {
                        _lineRenderer.sharedMaterial.color = lineColor;
                    }
                }
            }

#if UNITY_EDITOR
            //SceneView.RepaintAll();
#endif
        }
    }

    public IEnumerable<Vector3> OffsetPath(IEnumerable<GridNode> nodes, Vector3 origin, float amount)
    {
        foreach (var node in nodes)
        {
            yield return _gridManager.Grid.Transform.GetWorldPosition(node.NavigableCenter) + node.Normal * amount;
        }
    }

    public IEnumerable<GridNode> MergeLikeDirectionPathSegments(IEnumerable<GridNode> nodes)
    {
        GridNode last = default;
        var lastDir = Vector3.zero;
        foreach (var node in nodes)
        {
            var dir = node.NavigableCenter - last.NavigableCenter;
            if (dir != lastDir)
            {
                yield return node;
            }
            lastDir = dir;
        }
    }

    private Color GetRandomColor()
    {
        return new Color(
            Random.Range(0f, 1f),
            Random.Range(0f, 1f),
            Random.Range(0f, 1f)
        );
    }

    private Vector3 GetRandomVector3Xz(int size)
    {
        return new Vector3(
            Random.Range(-size, size),
            1,
            Random.Range(-size, size)
        );
    }

    private Vector3 GetWorldPosition(GridNode firstNode)
    {
        return _gridManager.Grid.Transform.GetWorldPosition(firstNode.NavigableCenter) + firstNode.Normal * pathOffset;
    }
}
