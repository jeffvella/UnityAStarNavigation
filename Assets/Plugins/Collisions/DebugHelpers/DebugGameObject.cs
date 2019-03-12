using System.Linq;
using UnityEngine;
using Vella.SimpleBurstCollision;

public class DebugGameObject
{
    public GameObject WrappedObject;
    public Collider Collider;
    public BurstBaseCollider BurstCollider;

    private bool _isColliding;
    private readonly Renderer _renderer;
    private readonly Color _normalColor = Color.white;
    private readonly Color _intersectColor = Color.blue;

    public bool IsColliding
    {
        get => _isColliding;
        set
        {
            if (_isColliding != value)
            {
                _renderer.sharedMaterial.color = value ? _intersectColor : _normalColor;
            }
            _isColliding = value;
        }
    }
    public DebugGameObject(GameObject go)
    {
        WrappedObject = go;
        Collider = go.GetComponent<Collider>();
        _renderer = go.GetComponent<Renderer>();
        _renderer.sharedMaterial = new Material(Shader.Find("Standard"));
        BurstCollider = new BurstBaseCollider(Collider);
    }

    public DebugGameObject(BurstSphereCollider sphere)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        DuplicateAndScaleMesh(go, sphere.Radius*2);
        go.transform.position = sphere.Center;
        var collider = go.AddComponent<SphereCollider>();
        collider.radius = sphere.Radius;
        Collider = collider;
        _renderer = go.GetComponent<Renderer>();
        _renderer.sharedMaterial = new Material(Shader.Find("Standard"));
        BurstCollider = sphere.ToBaseCollider(go.GetInstanceID());
        WrappedObject = go;
    }

    public void DuplicateAndScaleMesh(GameObject go, float scale)
    {
        var mf = go.GetComponent<MeshFilter>();
        var originalMesh = mf?.sharedMesh;
        if (originalMesh == null)
            return;

        var newMesh = new Mesh();
        var vertices = new Vector3[originalMesh.vertices.Length];
        for (var i = 0; i < vertices.Length; i++)
        {
            var vertex = originalMesh.vertices[i];
            vertex.x = vertex.x * scale;
            vertex.y = vertex.y * scale;
            vertex.z = vertex.z * scale;
            vertices[i] = vertex;
        }
        newMesh.SetVertices(vertices.ToList());
        newMesh.SetTriangles(originalMesh.GetTriangles(0),0);
        newMesh.SetNormals(originalMesh.normals.ToList());
        newMesh.RecalculateBounds();
        mf.sharedMesh = newMesh;
    }

    public override string ToString() => $"[{WrappedObject.name}] BurstBaseCollider={BurstCollider}";
}
