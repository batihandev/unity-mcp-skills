using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace BatihanDev.UnityCliCommands.Analysis
{
    internal static class ScriptAnalysisFacts
    {
        private static readonly HashSet<string> Callbacks = new HashSet<string>(new[] { "Awake", "Start", "Update", "FixedUpdate", "LateUpdate", "OnEnable", "OnDisable", "OnDestroy", "OnApplicationQuit", "OnTriggerEnter", "OnTriggerExit", "OnTriggerStay", "OnCollisionEnter", "OnCollisionExit", "OnCollisionStay", "OnMouseDown", "OnMouseUp", "OnMouseDrag", "OnMouseEnter", "OnMouseExit", "OnGUI", "OnDrawGizmos", "OnDrawGizmosSelected", "OnValidate", "Reset" }, StringComparer.Ordinal);
        internal static bool Eligible(Type type, bool graph)
        {
            if (!type.IsClass || type.ContainsGenericParameters || (graph && type.IsAbstract)) return false;
            if (typeof(MonoBehaviour).IsAssignableFrom(type) || typeof(UnityEngine.ScriptableObject).IsAssignableFrom(type)) return true;
            if (type.IsAbstract || type.Namespace == null) return false;
            return !(graph ? new[] { "Unity", "System", "Microsoft", "Mono" } : new[] { "Unity", "System" }).Any(prefix => type.Namespace.StartsWith(prefix, StringComparison.Ordinal));
        }
        internal static Type Resolve(string name, bool graph)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Supply a unique script class name or exact qualified type name.");
            var universe = AnalysisSceneObjects.LoadedTypes().Where(type => Eligible(type, graph)).ToArray();
            var qualified = universe.Where(type => type.FullName == name).ToArray();
            var matches = qualified.Length > 0 ? qualified : universe.Where(type => string.Equals(type.Name, name, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length != 1) throw new ArgumentException(matches.Length == 0 ? "The script type is unavailable or ineligible." : "Script type identity is ambiguous; choose a unique qualified type.");
            return matches[0];
        }
        internal static string Kind(Type type) => typeof(MonoBehaviour).IsAssignableFrom(type) ? "MonoBehaviour" : typeof(UnityEngine.ScriptableObject).IsAssignableFrom(type) ? "ScriptableObject" : "Class";
        internal static bool Candidate(FieldInfo field) => !field.IsStatic && !field.IsInitOnly && !field.IsLiteral && !field.IsNotSerialized && (field.IsPublic || field.IsDefined(typeof(SerializeField), false));
        internal static ScriptAnalysisField Field(FieldInfo field) => new ScriptAnalysisField { Name = field.Name, Type = Friendly(field.FieldType), DeclaringType = field.DeclaringType.FullName, SerializationCandidate = Candidate(field), IsPrivate = field.IsPrivate, IsStatic = field.IsStatic };
        internal static List<FieldInfo> DeclaredFields(Type type, bool includePrivate) => type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly | (includePrivate ? BindingFlags.NonPublic : 0)).Where(field => !field.Name.StartsWith("<", StringComparison.Ordinal)).OrderBy(field => field.Name, StringComparer.Ordinal).ToList();
        internal static List<string> Lifecycle(Type type) => typeof(MonoBehaviour).IsAssignableFrom(type) ? type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Where(method => Callbacks.Contains(method.Name)).Select(method => method.Name).Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToList() : new List<string>();
        internal static ScriptAnalysisInspection Inspect(Type type, bool privateMembers)
        {
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly | (privateMembers ? BindingFlags.NonPublic : 0);
            return new ScriptAnalysisInspection { Name = type.Name, FullName = type.FullName, Assembly = type.Assembly.GetName().Name, Kind = Kind(type), BaseClass = type.BaseType?.FullName,
                Fields = DeclaredFields(type, privateMembers).Select(Field).ToList(),
                Properties = type.GetProperties(flags).Where(property => property.CanRead && property.GetIndexParameters().Length == 0 && property.GetGetMethod(true) != null && (privateMembers || property.GetGetMethod(true).IsPublic)).OrderBy(property => property.Name, StringComparer.Ordinal).Select(property => new ScriptAnalysisProperty { Name = property.Name, Type = Friendly(property.PropertyType), CanWrite = property.CanWrite, IsPrivate = property.GetGetMethod(true).IsPrivate }).ToList(),
                Methods = type.GetMethods(flags).Where(method => !method.IsSpecialName).OrderBy(method => method.Name, StringComparer.Ordinal).ThenBy(method => method.ToString(), StringComparer.Ordinal).Select(method => new ScriptAnalysisMethod { Name = method.Name, ReturnType = Friendly(method.ReturnType), Parameters = string.Join(", ", method.GetParameters().Select(parameter => Friendly(parameter.ParameterType) + " " + parameter.Name)), IsPrivate = method.IsPrivate }).ToList(), UnityCallbacks = Lifecycle(type) };
        }
        internal static List<ScriptAnalysisEdge> Edges(IEnumerable<Type> sources, IReadOnlyList<Type> universe)
        {
            var eligible = new HashSet<Type>(universe.Where(type => Eligible(type, true)));
            var result = new List<ScriptAnalysisEdge>();
            foreach (var source in sources.Distinct().OrderBy(type => type.FullName, StringComparer.Ordinal))
            {
                var fields = source.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).ToList();
                for (var current = source; current != null; current = current.BaseType)
                    fields.AddRange(current.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(field => Candidate(field) && field.IsDefined(typeof(SerializeField), false)));
                foreach (var field in fields.Distinct())
                {
                    var target = field.FieldType;
                    if (target.IsArray) target = target.GetElementType();
                    if (target != null && target.IsGenericType) target = target.GetGenericArguments().FirstOrDefault();
                    if (target == null || target == source || !eligible.Contains(target)) continue;
                    result.Add(new ScriptAnalysisEdge { From = source.FullName, To = target.FullName, SourceType = source.FullName, DeclaringType = field.DeclaringType.FullName, Field = field.Name, FieldType = target.FullName });
                }
            }
            return result.GroupBy(edge => string.Join("|", edge.SourceType, edge.DeclaringType, edge.Field, edge.To), StringComparer.Ordinal).Select(group => group.First()).OrderBy(edge => edge.From, StringComparer.Ordinal).ThenBy(edge => edge.Field, StringComparer.Ordinal).ThenBy(edge => edge.To, StringComparer.Ordinal).ToList();
        }
        internal static Dictionary<Type, string> SourceMap(bool assetsOnly)
        {
            var map = new Dictionary<Type, string>();
            foreach (var guid in assetsOnly ? AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" }) : AssetDatabase.FindAssets("t:MonoScript"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid); var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path); var type = script == null ? null : script.GetClass();
                if (type != null && (!map.ContainsKey(type) || string.CompareOrdinal(path, map[type]) < 0)) map[type] = path;
            }
            return map;
        }
        internal static string Friendly(Type type)
        {
            if (type.IsArray) return Friendly(type.GetElementType()) + "[]";
            if (type.IsGenericType) return type.Name.Split('`')[0] + "<" + string.Join(", ", type.GetGenericArguments().Select(Friendly)) + ">";
            return type == typeof(int) ? "int" : type == typeof(float) ? "float" : type == typeof(bool) ? "bool" : type == typeof(string) ? "string" : type == typeof(void) ? "void" : type.FullName;
        }
    }
}
