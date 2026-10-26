using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BatihanDev.UnityCliCommands.Foundation;
using Unity.Pipeline.Commands;
using UnityEngine;
using UnityComponent = UnityEngine.Component;
using Exact = BatihanDev.UnityCliCommands.Editor.ExactObjectReference;
namespace BatihanDev.UnityCliCommands.Analysis
{
    public static class SmartQueryCommands
    {
        [CliCommand("smart.query", "Smart query operation.", Tags = new[] { "unity-cli-commands", "smart" })]
        public static CommandResult<SmartQueryReport> Query([CliArg("componentName", "Unique qualified Component type name.")] string componentName = null, [CliArg("propertyName", "Exact public instance field or readable nonindexer property name.")] string propertyName = null, [CliArg("op", "==, !=, >, <, >=, <= or contains.")] string op = "==", [CliArg("value", "Required comparison operand; empty string is valid, null is refused.")] string value = null, [CliArg("limit", "Nonnegative shown result cap; full totals and diagnostics remain available.")] int limit = 50, [CliArg("query", "Unsupported shorthand; use explicit component/member/operator/value.")] string query = null)
        {
            const string schema = "unity.smart.query@1";
            try
            {
                SmartSelectionFacts.Compatible();
                if (query != null || value == null || string.IsNullOrWhiteSpace(propertyName) || limit < 0 || !new[] { "==", "!=", ">", "<", ">=", "<=", "contains" }.Contains(op))
                    throw new SmartFailure("SMART_INPUT_INVALID", "Supply componentName/propertyName/op/value and a nonnegative limit; query shorthand is unsupported.");
                var type = ComponentType(componentName);
                var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
                var property = field == null ? type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance) : null;
                if (field == null && (property == null || property.GetIndexParameters().Length != 0 || property.GetGetMethod(false) == null))
                    throw new SmartFailure("SMART_MEMBER_INVALID", "Choose an exact public instance field or readable nonindexer property.");
                var relational = op == ">" || op == "<" || op == ">=" || op == "<=";
                if (relational && !TryFinite(value, out _)) throw new SmartFailure("SMART_INPUT_INVALID", "Relational comparison requires a finite invariant numeric operand.");
                var memberType = field != null ? field.FieldType : property.PropertyType;
                if ((op == "==" || op == "!=") && IsNumeric(memberType) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var operand) && !SmartSelectionFacts.Finite(operand))
                    throw new SmartFailure("SMART_INPUT_INVALID", "Numeric comparison operands must be finite.");
                var report = new SmartQueryReport { Predicate = type.FullName + "." + propertyName + " " + op + " " + value };
                foreach (var component in AnalysisSceneObjects.NativeComponents(type, sortMode: FindObjectsSortMode.InstanceID))
                    {
                        var go = component.gameObject;
                        object actual;
                        try { actual = field != null ? field.GetValue(component) : property.GetValue(component, null); }
                        catch (Exception exception)
                        {
                            report.Diagnostics.Add(new SmartQueryDiagnostic { Component = Exact.ExactId(component), Code = "GETTER_FAILED", Message = (exception.InnerException ?? exception).Message }); continue;
                        }
                        if (actual == null) continue;
                        string formatted;
                        try { formatted = actual is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : Convert.ToString(actual, CultureInfo.InvariantCulture); }
                        catch (Exception exception) { report.Diagnostics.Add(new SmartQueryDiagnostic { Component = Exact.ExactId(component), Code = "FORMAT_FAILED", Message = exception.Message }); continue; }
                        if (!Compare(actual, formatted, op, value)) continue;
                        report.Total++;
                        if (report.Items.Count < limit) report.Items.Add(new SmartQueryItem { Component = Exact.ExactId(component), GameObject = Exact.ExactId(go), Name = go.name, Path = AnalysisSceneObjects.Path(go), Value = formatted });
                    }
                report.Truncated = report.Total > report.Items.Count;
                return CommandResult<SmartQueryReport>.Success(schema, report);
            }
            catch (Exception failure) { return SmartSelectionFacts.Failure<SmartQueryReport>(schema, failure); }
        }
        [CliCommand("smart.spatial", "Smart spatial operation.", Tags = new[] { "unity-cli-commands", "smart" })]
        public static CommandResult<SmartSpatialReport> Spatial([CliArg("x", "Finite world sphere center X.")] float x, [CliArg("y", "Finite world sphere center Y.")] float y, [CliArg("z", "Finite world sphere center Z.")] float z, [CliArg("radius", "Finite nonnegative Physics overlap sphere radius.")] float radius = 10, [CliArg("componentFilter", "Optional unique Component type filter; invalid types refuse.")] string componentFilter = null, [CliArg("limit", "Nonnegative shown result cap; full totals and diagnostics remain available.")] int limit = 50)
        {
            const string schema = "unity.smart.spatial@1";
            try
            {
                SmartSelectionFacts.Compatible(); var center = new Vector3(x, y, z);
                if (!SmartSelectionFacts.Finite(center) || limit < 0) throw new SmartFailure("SMART_INPUT_INVALID", "Use a finite center and nonnegative result limit.");
                SmartSelectionFacts.Nonnegative(radius, "radius");
                var type = componentFilter == null ? null : ComponentType(componentFilter);
                var eligible = AnalysisSceneObjects.Loaded(); var report = new SmartSpatialReport { Center = BatihanDev.UnityCliCommands.Transform.TransformCommands.Vector(center), Radius = radius };
                foreach (var collider in Physics.OverlapSphere(center, radius))
                {
                    if (collider == null || !eligible.Contains(collider.gameObject) || (type != null && collider.GetComponent(type) == null)) continue;
                    report.Total++; var go = collider.gameObject;
                    if (report.Items.Count < limit) report.Items.Add(new SmartSpatialItem { Collider = Exact.ExactId(collider), GameObject = Exact.ExactId(go), Name = go.name, Path = AnalysisSceneObjects.Path(go), Distance = Vector3.Distance(center, go.transform.position) });
                }
                report.Truncated = report.Total > report.Items.Count;
                return CommandResult<SmartSpatialReport>.Success(schema, report);
            }
            catch (Exception failure) { return SmartSelectionFacts.Failure<SmartSpatialReport>(schema, failure); }
        }

        private static Type ComponentType(string name)
        {
            try { return AnalysisSceneObjects.ResolveType(name, typeof(UnityComponent)); }
            catch (Exception exception) { throw new SmartFailure("SMART_TYPE_INVALID", exception.Message); }
        }
        private static bool TryFinite(string text, out float value) => float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && SmartSelectionFacts.Finite(value);
        private static bool IsNumeric(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type == typeof(float) || type == typeof(double) || type == typeof(decimal) || type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong);
        }
        private static bool Compare(object actual, string formatted, string op, string operand)
        {
            if (op == "contains") return formatted.IndexOf(operand, StringComparison.OrdinalIgnoreCase) >= 0;
            var leftNumeric = TryFinite(formatted, out var left); var rightNumeric = TryFinite(operand, out var right);
            if (IsNumeric(actual.GetType()) && !leftNumeric) throw new SmartFailure("SMART_INPUT_INVALID", "A numeric member is nonfinite or outside the supported finite comparison range.");
            switch (op)
            {
                case "==": return leftNumeric && rightNumeric ? Math.Abs(left - right) < .0001f : string.Equals(formatted, operand, StringComparison.Ordinal);
                case "!=": return leftNumeric && rightNumeric ? Math.Abs(left - right) >= .0001f : !string.Equals(formatted, operand, StringComparison.Ordinal);
                case ">": return leftNumeric && rightNumeric && left > right;
                case "<": return leftNumeric && rightNumeric && left < right;
                case ">=": return leftNumeric && rightNumeric && left >= right;
                case "<=": return leftNumeric && rightNumeric && left <= right;
                default: return false;
            }
        }
    }
}
