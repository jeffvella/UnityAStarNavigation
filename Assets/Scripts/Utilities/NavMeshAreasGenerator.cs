using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Runtime.CompilerServices;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.AI
{
    #region Auto-Generated Content

        [Flags]
        public enum NavMeshAreas
        {
            None = 0,
            Walkable = 1, NotWalkable = 2, Jump = 4,
            All = ~0,
        }

    #endregion

#if UNITY_EDITOR

    /// <summary>
    /// Auto-updates the <see cref="NavMeshAreas"/> enum in this file if it has changed, when scripts are compiled or assets saved.
    /// </summary>
    public static class NavMeshAreasGenerator
    {
        private const string EnumValuesToken = "#EnumValues";
        private const string HashSettingsKey = "NavMeshAreasHash";

        private static void Update([CallerFilePath] string executingFilePath = "")
        {
            var areaNames = GameObjectUtility.GetNavMeshAreaNames();
            var lastHash = EditorPrefs.GetInt(HashSettingsKey);
            var newHash = GetAreaHash(areaNames);

            if (newHash != lastHash)
            {
                Debug.Log($"{nameof(NavMeshAreas)} have changed, updating enum: '{executingFilePath}'");
                GenerateFile(areaNames, newHash, executingFilePath);
            }
        }

        private static int GetAreaHash(string[] areaNames)
        {
            var input = areaNames.Aggregate((a, b) => a + b);
            var hash = 0;
            foreach (var t in input)
                hash = (hash << 5) + hash + t;
            return hash;
        }

        private static void GenerateFile(string[] areaNames = default, int hash = 0, string outputPath = null)
        {
            if (areaNames == null)
                areaNames = GameObjectUtility.GetNavMeshAreaNames();

            if (hash == 0)
                hash = GetAreaHash(areaNames);

            var values = GetAreaEnumValuesAsText(ref areaNames);
            var newEnumText = ContentTemplate.Replace(EnumValuesToken, values);
            var output = ReplaceEnumInFile(nameof(NavMeshAreas), File.ReadAllLines(outputPath), newEnumText);

            CreateScriptAssetWithContent(outputPath, string.Concat(output));
            EditorPrefs.SetInt(HashSettingsKey, hash);
            AssetDatabase.Refresh();
        }

        private static string GetAreaEnumValuesAsText(ref string[] areaNames)
        {
            var increment = 0;
            var output = new StringBuilder();
            var seenKeys = new HashSet<string>();

            foreach (var name in areaNames)
            {
                var enumKey = string.Concat(name.Where(char.IsLetterOrDigit));
                var value = 1 << NavMesh.GetAreaFromName(name);

                output.Append(seenKeys.Contains(name)
                    ? $"{(enumKey + increment++)} = {value}, "
                    : $"{enumKey} = {value}, ");

                seenKeys.Add(enumKey);
            }
            return output.ToString();
        }

        private static readonly string ContentTemplate =
        $@"        public enum {nameof(NavMeshAreas)}
        {{
            None = 0,               
            {EnumValuesToken}
            All = ~0,
        }}
        ";

        private static string ReplaceEnumInFile(string enumName, string[] fileLines, string newEnum)
        {
            int enumStartLine = 0, enumEndLine = 0;
            var result = new StringBuilder();
            for (int i = 0; i < fileLines.Length; i++)
            {
                string line = fileLines[i];
                if (line.Trim().StartsWith("public enum " + enumName))
                {
                    enumStartLine = i;
                    break;
                }
                result.AppendLine(line);
            }
            if (enumStartLine > 0)
            {
                for (int i = enumStartLine + 1; i < fileLines.Length; i++)
                {
                    if (fileLines[i].Contains("}"))
                    {
                        enumEndLine = i;
                        break;
                    }
                }
                result.Append(newEnum);
                for (int i = enumEndLine + 1; i < fileLines.Length; i++)
                {
                    result.AppendLine(fileLines[i]);
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// Create a new script asset.
        /// UnityEditor.ProjectWindowUtil.CreateScriptAssetWithContent (2019.1)
        /// </summary>
        /// <param name="pathName">the path to where the new file should be created</param>
        /// <param name="templateContent">the text to put inside</param>
        /// <returns></returns>
        private static UnityEngine.Object CreateScriptAssetWithContent(string pathName, string templateContent)
        {
            templateContent = SetLineEndings(templateContent, EditorSettings.lineEndingsForNewScripts);
            string fullPath = Path.GetFullPath(pathName);
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding(true);
            File.WriteAllText(fullPath, templateContent, encoding);
            AssetDatabase.ImportAsset(pathName);
            return AssetDatabase.LoadAssetAtPath(pathName, typeof(UnityEngine.Object));
        }

        /// <summary>
        /// Ensure correct OS specific line endings for saving file content.
        /// UnityEditor.ProjectWindowUtil.SetLineEndings (2019.1)
        /// </summary>
        /// <param name="content">a string to have line endings checked</param>
        /// <param name="lineEndingsMode">the type of line endings to use</param>
        /// <returns>a cleaned string</returns>
        private static string SetLineEndings(string content, LineEndingsMode lineEndingsMode)
        {
            string replacement;
            switch (lineEndingsMode)
            {
                case LineEndingsMode.OSNative:
                    replacement = Application.platform == RuntimePlatform.WindowsEditor ? "\r\n" : "\n";
                    break;
                case LineEndingsMode.Unix:
                    replacement = "\n";
                    break;
                case LineEndingsMode.Windows:
                    replacement = "\r\n";
                    break;
                default:
                    replacement = "\n";
                    break;
            }
            content = System.Text.RegularExpressions.Regex.Replace(content, "\\r\\n?|\\n", replacement);
            return content;
        }


        /// <summary>
        /// Hook that runs the enum generator whenever assets are saved.
        /// </summary>
        private class UpdateOnAssetModification : UnityEditor.AssetModificationProcessor
        {
            public static string[] OnWillSaveAssets(string[] paths)
            {
                Update();
                return paths;
            }
        }

        /// <summary>
        /// Hook that runs the enum generator whenever scripts are compiled.
        /// </summary>
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void UpdateOnScriptCompile()
        {
            Update();
        }

        /// <summary>
        /// Enables manually running the enum generator from the menus.
        /// </summary>
        [MenuItem("Tools/Update NavMeshAreas")]
        private static void UpdateOnMenuCommand()
        {
            UpdateOnScriptCompile();
        }

    }

    /// <summary>
    /// Flags enum dropdown GUI for selecting <see cref="NavMeshAreas"/> properties in the inspector
    /// </summary>
    [CustomPropertyDrawer(typeof(NavMeshAreas))]
    public class NavMeshAreasDrawer : PropertyDrawer
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

    /// <summary>
    /// A helper for flag operations with NavMeshAreas
    /// </summary>
    public struct AreaMask
    {
        private readonly int _value;


        public int Value => _value;
        public NavMeshAreas EnumValue => (NavMeshAreas)_value;

        public AreaMask(int value)
        {
            _value = value;
        }

        public AreaMask(NavMeshAreas areas)
        {
            _value = (int)areas;
        }

        public static implicit operator AreaMask(int value) => new AreaMask(value);
        public static implicit operator AreaMask(string name) => new AreaMask(1 << NavMesh.GetAreaFromName(name));
        public static implicit operator AreaMask(NavMeshAreas areas) => new AreaMask((int)areas);
        public static implicit operator NavMeshAreas(AreaMask flag) => (NavMeshAreas)flag._value;
        public static implicit operator int(AreaMask flag) => flag._value;

        public static bool operator ==(AreaMask a, int b) => a._value.Equals(b);
        public static bool operator !=(AreaMask a, int b) => !a._value.Equals(b);
        public static int operator +(AreaMask a, AreaMask b) => a.Add(b._value);
        public static int operator -(AreaMask a, AreaMask b) => a.Remove(b._value);
        public static int operator |(AreaMask a, AreaMask b) => a.Add(b._value);
        public static int operator ~(AreaMask a) => ~a._value;
        public static int operator +(int a, AreaMask b) => a |= b._value;
        public static int operator -(int a, AreaMask b) => a &= ~b._value;
        public static int operator |(int a, AreaMask b) => a |= b._value;
        public static int operator +(AreaMask a, int b) => a.Add(b);
        public static int operator -(AreaMask a, int b) => a.Remove(b);
        public static int operator |(AreaMask a, int b) => a.Add(b);

        public bool HasFlag(AreaMask flag) => (_value & flag._value) == flag;
        public bool HasFlag(int value) => (_value & value) == value;
        public AreaMask Add(AreaMask flag) => _value | flag._value;
        public AreaMask Remove(AreaMask flag) => _value & ~flag._value;
        public AreaMask Add(NavMeshAreas flags) => _value | (int)flags;
        public AreaMask Remove(NavMeshAreas flags) => _value & ~(int)flags;

        public bool Equals(AreaMask other) => _value == other._value;
        public override string ToString() => ((NavMeshAreas)_value).ToString();
        public override int GetHashCode() => _value;
        public override bool Equals(object obj)
            => !ReferenceEquals(null, obj) && (obj is AreaMask other && Equals(other));


        private static Array _areas;
        public static int GetIndex(NavMeshAreas area)
        {
            return Array.IndexOf(_areas ?? (_areas = Enum.GetValues(typeof(NavMeshAreas))), area)-1;
        }
    }

}


