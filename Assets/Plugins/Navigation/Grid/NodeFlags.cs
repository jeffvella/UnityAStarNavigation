using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace Providers.Grid
{
    [Flags]
    public enum NodeFlags : int
    {
        None = 0x00000000,
        AllowWalk = 0x00000001,
        AllowFlier = 0x00000002,
        AllowProjectile = 0x00000004,
        RayCast = 0x00000008,
        Navigation = 0x00000010,
        Combat = 0x00000020,
        Avoidance = 0x00000040,
        Obstacle = 0x00000080,
        Ranged = 0x00000100,
        NearEdge = 0x00000200,
        Pierce = 0x00000400,
        Monster = 0x00000800,
        Health = 0x00001000,
        //NearEdge = 0x00002000,
        //ProjectileBlocking = 0x00004000,
        //CriticalAvoidance = 0x00008000,
        //Backtrack = 0x00010000,
        //NearEdge = 0x00020000,
        //Obstacle = 0x00040000,
        //Unused20 = 0x00080000,
        //Unused21 = 0x00100000,
        //Unused22 = 0x00200000,
        //Unused23 = 0x00400000,
        //Unused24 = 0x00800000,
        //Unused25 = 0x01000000,
        //Unused26 = 0x02000000,
        //Unused27 = 0x04000000,
        //Unused28 = 0x08000000,
        //Unused29 = 0x10000000,
        //Unused30 = 0x20000000,
        //Unused31 = 0x40000000,
        //Unused32 = 0x80000000,
        //Unused33 = 0x100000000,
        //Unused34 = 0x200000000,
        //Unused35 = 0x400000000,
        //Unused36 = 0x800000000,
        //Unused37 = 0x1000000000,
        //Unused38 = 0x2000000000,
        //Unused39 = 0x4000000000,
        //Unused40 = 0x8000000000,
        //Unused41 = 0x10000000000,
        //Unused42 = 0x20000000000,
        //Unused43 = 0x40000000000,
        //Unused44 = 0x80000000000,
        //Unused45 = 0x100000000000,
        //Unused46 = 0x200000000000,
        //Unused47 = 0x400000000000,
        //Unused48 = 0x800000000000,
        //Unused49 = 0x1000000000000,
        //Unused50 = 0x2000000000000,
        //Unused51 = 0x4000000000000,
        //Unused52 = 0x8000000000000,
        //Unused53 = 0x10000000000000,
        //Unused54 = 0x20000000000000,
        //Unused55 = 0x40000000000000,
        //Unused56 = 0x80000000000000,
        //Unused57 = 0x100000000000000,
        //Unused58 = 0x200000000000000,
        //Unused59 = 0x400000000000000,
        //Unused60 = 0x800000000000000,
        //Unused61 = 0x1000000000000000,
        //Unused62 = 0x2000000000000000,
        //Unused63 = 0x4000000000000000,
        //Unused64 = 0x8000000000000000,
    }

#if UNITY_EDITOR
    /// <summary>
    /// Flags enum dropdown GUI for selecting <see cref="NavMeshAreas"/> properties in the inspector
    /// </summary>
    [CustomPropertyDrawer(typeof(NodeFlags))]
    public class NodeFlagsDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            var oldValue = (Enum)fieldInfo.GetValue(property.serializedObject.targetObject);
            var newValue = EditorGUI.EnumFlagsField(position, label, oldValue);
            if (!newValue.Equals(oldValue))
            {
                property.intValue = (int)Convert.ChangeType(newValue, fieldInfo.FieldType);
            }
            EditorGUI.EndProperty();
        }
    }
#endif
}
