using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Unity.Mathematics;

namespace Providers.Grid
{
    /// <summary>
    /// A basic version of 'Transform' component using Unity.Mathematics so that it can be executed outside of Unity's environment.
    /// Doesn't combined world position from a transform hierarchy.
    /// </summary>
    public class SimpleTransform
    {        
        private RigidTransform _transform;
        private Vector3 _scale;
        private float4x4 _toWorldMatrix;
        private float4x4 _toLocalMatrix;

        public Vector3 LocalPosition => _transform.pos;
        public Quaternion LocalRotation => _transform.rot;
        public Vector3 LocalScale => _scale;

        public float4x4 ToWorldMatrix => _toWorldMatrix;
        public float4x4 ToLocalMatrix => _toLocalMatrix;

        public Vector3 GetWorldPosition(Vector3 localPosition)
        {            
            return math.transform(_toWorldMatrix, localPosition);
        }

        public Vector3 GetLocalPosition(Vector3 worldPosition)
        {
            return math.transform(_toLocalMatrix, worldPosition);
        }

        public void Set(Transform transform)
        {
            _scale = transform.localScale;
            _transform.pos = transform.position;
            _transform.rot = transform.rotation;
            _toWorldMatrix = transform.localToWorldMatrix;
            _toLocalMatrix = transform.worldToLocalMatrix;
        }

        public void Set(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            _scale = scale;
            _transform.pos = position;
            _transform.rot = rotation; 
            _toWorldMatrix = math.mul(new float4x4(_transform), float4x4.Scale(scale));
            _toLocalMatrix = math.inverse(_toWorldMatrix);
        }
    }


    ///// <summary>
    ///// A basic version of 'Transform' component using Unity.Mathematics so that it can be executed outside of Unity's environment.
    ///// </summary>
    //public class SimpleTransformNoScale
    //{
    //    private RigidTransform _transform;
    //    private Vector3 _scale;

    //    public Vector3 LocalPosition => _transform.pos;
    //    public Quaternion LocalRotation => _transform.rot;
    //    public Vector3 GetWorldPosition(Vector3 localPosition)
    //    {
    //        return math.transform(_transform, localPosition);
    //    }

    //    public Vector3 GetLocalPosition(Vector3 worldPosition)
    //    {
    //        return math.transform(math.inverse(_transform), worldPosition);
    //    }

    //    public void Set(Vector3 position, Quaternion rotation)
    //    {
    //        _transform.pos = position;
    //        _transform.rot = rotation;
    //        NotifyChanged();
    //    }

    //    private void NotifyChanged()
    //    {
    //        LastChanged = DateTime.UtcNow;
    //    }

    //    public DateTime LastChanged { get; private set; } = DateTime.MinValue;

    //    public Matrix4x4 Matrix => Matrix4x4.TRS(_transform.pos, _transform.rot, Vector3.one);

    //}

}

