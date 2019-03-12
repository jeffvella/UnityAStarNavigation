using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Tilemaps;
using Vella.SimpleBurstCollision;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class CollisionTester : MonoBehaviour
{
    public List<GameObject> SourceObjects;
    private readonly Dictionary<int, DebugGameObject> _trackedObjects = new Dictionary<int, DebugGameObject>();
    private Stopwatch _stopwatch = new Stopwatch();
    private List<TimeSpan> _timings = new List<TimeSpan>();

    //private NativeArray<BurstBaseCollider> _bulkColliders;

    void OnEnable()
    {
        //_bulkColliders = new NativeArray<BurstBaseCollider>(10000, Allocator.Persistent);

        //for (int i = 0; i < _bulkColliders.Length; i++)
        //{
        //    _bulkColliders[i] = i % 2 == 0 ? CreateRandomBox().ToBaseCollider() : CreateRandomSphere().ToBaseCollider();
        //}
    }

    void OnDestroy()
    {
        //_bulkColliders.Dispose();
    }

    private DateTime _lastUpdateTime = DateTime.MinValue;

    void Update()
    {
        if (DateTime.UtcNow.Subtract(_lastUpdateTime).TotalMilliseconds < 250)
            return;

        _lastUpdateTime = DateTime.UtcNow;

        UpdateCollection(SourceObjects);

        foreach (var item in _trackedObjects)
        {
            item.Value.IsColliding = false;
            item.Value.BurstCollider.Update(item.Value.Collider);
        }

        _timings.Clear();

        foreach (var item in _trackedObjects)
        {
            var others = _trackedObjects.Where(kvp => kvp.Key != item.Key).Select(kvp => kvp.Value.BurstCollider).ToArray();

            _stopwatch.Restart();
            using (var intersections = item.Value.BurstCollider.Intersects(others))
            {
                for (int i = 0; i < intersections.Length; i++)
                {
                    _trackedObjects[intersections[i].Id].IsColliding = true;
                }
            }
            _stopwatch.Stop();
            _timings.Add(_stopwatch.Elapsed);
        }

        Debug.Log($"{_timings.Count} Intersection jobs ({((_trackedObjects.Count-1)*(_trackedObjects.Count-1))}) took on average {_timings.Average(t => t.TotalMilliseconds):N4} ms ({_timings.Sum(t => t.TotalMilliseconds):N2} ms total)");
    }

    private void UpdateCollection(IEnumerable<GameObject> source, bool pruneToSource = false)
    {
        var seenKeys = new HashSet<int>(_trackedObjects.Keys);    
        foreach (var go in source)
        {
            if (go == null)
            {
                continue;
            }
            var key = go.GetInstanceID();
            if (!_trackedObjects.ContainsKey(key))
            {
                _trackedObjects.Add(key, new DebugGameObject(go));
            }            
            seenKeys.Remove(key);
        }
        if (pruneToSource)
        {
            foreach (var key in seenKeys)
            {
                _trackedObjects.Remove(key);
            }
        }
        else
        {
            foreach (var item in _trackedObjects.ToList())
            {
                if (item.Value == null)
                {
                    _trackedObjects.Remove(item.Key);
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        foreach (var item in _trackedObjects)
        {
            var box = BurstColliderFactory.CreateBox(item.Value.WrappedObject.transform);
            DrawingHelpers.DrawWireFrame(box);
        }    
    }

    public static BurstSphereCollider CreateRandomSphere()
    {
        var x = BurstColliderFactory.CreateSphere(Random.insideUnitSphere * 2, Random.value);
        return x;
    }

    public static BurstBoxCollider CreateRandomBox()
    {
        var randomPosition = new Vector3(Random.value, Random.value, Random.value);
        var x = BurstColliderFactory.CreateBox(Random.insideUnitSphere * 4, randomPosition, Random.rotation);
        return x;
    }

}