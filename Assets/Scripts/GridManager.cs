using System;
using System.Collections.Generic;
using System.Linq;
using Providers.Grid;
using UnityEngine;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class GridManager : MonoBehaviour
{
    [Header("Grid Options")]
    public NodeFlags DefaultFlags = NodeFlags.AllowWalk;
    public float BoxSize = 2.5f;
    public Vector3 Size;

    public List<GridVolumeModifier> Obstacles;

    [Header("NavMeshTracing Options")]
    public float RequiredProximity = 0.5f;
    public float EdgeWidth = 0.2f;

    public bool RequireVerticalAlign = true;
    public bool ShowGridBounds = true;
    public bool DrawGridPoints = true;

    public NavigationGrid Grid { get; set; }

    public bool IsValid => Grid != null &&  Grid.NodeCount > 0 && !Grid.InnerGrid.IsDisposed;

    private void Awake()
    {
        BuildGrid();
    }

    public void BuildGrid()
    {
        Debug.Log("GridManager.BuildGrid");

        if (BoxSize < 0.1f)
            BoxSize = 1;


        Grid?.Dispose();
        Grid = NavigationGrid.Create((int) Size.x, (int) Size.y, (int) Size.z, transform.position, transform.rotation, transform.localScale, BoxSize);

        UpdateWalkableAreas();
    }

    public Vector3 GetVertexWorldPosition(Vector3 vertex, Transform owner)
    {
        return owner.localToWorldMatrix.MultiplyPoint3x4(vertex);
    }

    public void UpdateWalkableAreas()
    {
        GridNodeJobs.AllNodes.TraceNavMesh.Execute(Grid, RequiredProximity, EdgeWidth, RequireVerticalAlign, NodeFlags.Navigation, NodeFlags.NearEdge, DefaultFlags);
    }

    private void Update()
    {
        if (Grid == null)
        {
            BuildGrid();
        }

        if (DrawGridPoints)
        {
            foreach (var node in Grid.InnerGrid)
            {
                if (node.HasFlag(NodeFlags.Avoidance))
                {
                    DebugExtension.DebugWireSphere(Grid.ToWorldPosition(node.NavigableCenter), UnityColors.OrangeRed, 0.1f);
              
                }
                else if (node.HasFlag(NodeFlags.NearEdge))
                {
                    DebugExtension.DebugPoint(Grid.ToWorldPosition(node.NavigableCenter), UnityColors.Gray, 0.2f);
                }
                else if (node.HasFlag(NodeFlags.Navigation))
                {
                    DebugExtension.DebugPoint(Grid.ToWorldPosition(node.NavigableCenter), UnityColors.Blue, 0.2f);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (Grid != null)
        {          
            Gizmos.matrix = Grid.Transform.ToWorldMatrix;

            if (ShowGridBounds)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawWireCube(Grid.LocalDataBounds.center* Grid.BoxSize + Grid.BoxOffset, Grid.LocalDataBounds.size* Grid.BoxSize);
            }
        }
    }

    void OnEnable()
    {
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
#endif
    }

#if UNITY_EDITOR
    private void EditorApplication_playModeStateChanged(PlayModeStateChange state)
    {
        switch (state)
        {
            case PlayModeStateChange.ExitingEditMode:
            case PlayModeStateChange.ExitingPlayMode:
                EnsureDestroyed();
                break;
        }
    }
#endif

    void OnDestroy() => EnsureDestroyed();
    void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;
#endif
        EnsureDestroyed();
    }

    private void EnsureDestroyed()
    {
        Grid?.Dispose();
        NavMeshExtensions.Dispose();
    }

}

#if UNITY_EDITOR

[CustomEditor(typeof(GridManager))]
[CanEditMultipleObjects]
public class GridManager_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        foreach (var targ in targets.Cast<GridManager>())
        {
            var grid = targ.Grid;

            if (GUILayout.Button("Generate Grid") || grid == null)
            {
                targ.BuildGrid();
            }

            if (GUILayout.Button("Update Edges") || grid == null)
            {
                NavMeshExtensions.CalculateEdges(UnityEngine.AI.NavMesh.CalculateTriangulation());
                SceneView.RepaintAll();
            }

            if (grid != null)
            {
                GUILayout.Label($"Nodes: {grid.InnerGrid.Length}");
                GUILayout.Label($"InnerArraySize: {grid.InnerGrid.Length}");
            }
        }
    }
}

#endif