# ComponentSkills.ConvertValue Helper

Paste-in string→typed-value converter. Supports primitives, Unity value types
(Vector2/3/4, Vector2Int/3Int, Quaternion, Color, Color32, Rect, Bounds,
LayerMask, AnimationCurve), and enums.

## Supported input formats

- **Bool** — `true`, `false`, `1`, `0`, `yes`, `on` (case-insensitive).
- **Vectors** — `(x, y)`, `[x, y, z]`, `x, y`, `x y`. Parens / brackets / commas / spaces / semicolons all accepted.
- **Quaternion** — 3 floats (Euler angles, degrees) or 4 floats (xyzw).
- **Color** — `#RRGGBB[AA]` hex, named colors (`red`, `cyan`, `gray`, …), or `r, g, b[, a]` float tuple (0–1).
- **Rect** — 4 floats `x, y, width, height`.
- **Bounds** — 6 floats: center xyz then size xyz.
- **LayerMask** — layer name or integer bitmask.
- **AnimationCurve** — `linear`, `easein`, `easeout`, `easeinout`, `constant`.
- **Enum** — member name (case-insensitive).
- **Primitives** — `int`, `float`, `double`, `long`, `string`.

## Call pattern

```csharp
try
{
    var converted = ComponentSkills.ConvertValue(value, field.FieldType);
    field.SetValue(target, converted);
}
catch (System.Exception ex)
{
    result.SetResult(new { error = $"Cannot convert '{value}' to {field.FieldType.Name}: {ex.Message}" });
    return;
}
```

## Do not

- Do not paste both this file and `component_type_finder.md` in the same
  `Unity_RunCommand` — both define `internal static class ComponentSkills`.

## Paste-in

```csharp
    internal static class ComponentSkills
    {
        public static object ConvertValue(string value, System.Type targetType)
        {
            if (value == null || value.Equals("null", System.StringComparison.OrdinalIgnoreCase))
                return targetType.IsValueType ? System.Activator.CreateInstance(targetType) : null;

            if (targetType == typeof(string)) return value;
            if (targetType == typeof(int)) return int.Parse(value);
            if (targetType == typeof(float)) return float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
            if (targetType == typeof(double)) return double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
            if (targetType == typeof(bool)) return ParseBool(value);
            if (targetType == typeof(long)) return long.Parse(value);

            if (targetType == typeof(UnityEngine.Vector2)) return ParseVector2(value);
            if (targetType == typeof(UnityEngine.Vector3)) return ParseVector3(value);
            if (targetType == typeof(UnityEngine.Vector4)) return ParseVector4(value);
            if (targetType == typeof(UnityEngine.Vector2Int)) return ParseVector2Int(value);
            if (targetType == typeof(UnityEngine.Vector3Int)) return ParseVector3Int(value);

            if (targetType == typeof(UnityEngine.Quaternion)) return ParseQuaternion(value);
            if (targetType == typeof(UnityEngine.Color)) return ParseColor(value);
            if (targetType == typeof(UnityEngine.Color32)) return ParseColor32(value);
            if (targetType == typeof(UnityEngine.Rect)) return ParseRect(value);
            if (targetType == typeof(UnityEngine.Bounds)) return ParseBounds(value);
            if (targetType == typeof(UnityEngine.LayerMask)) return ParseLayerMask(value);

            if (targetType.IsEnum)
                return System.Enum.Parse(targetType, value, true);

            if (targetType == typeof(UnityEngine.AnimationCurve))
                return ParseAnimationCurve(value);

            return System.Convert.ChangeType(value, targetType);
        }

        private static bool ParseBool(string value)
        {
            value = value.ToLower().Trim();
            return value == "true" || value == "1" || value == "yes" || value == "on";
        }

        private static UnityEngine.Vector2 ParseVector2(string value)
        {
            var parts = ParseFloatArray(value, 2);
            return new UnityEngine.Vector2(parts[0], parts[1]);
        }

        private static UnityEngine.Vector3 ParseVector3(string value)
        {
            var parts = ParseFloatArray(value, 3);
            return new UnityEngine.Vector3(parts[0], parts[1], parts[2]);
        }

        private static UnityEngine.Vector4 ParseVector4(string value)
        {
            var parts = ParseFloatArray(value, 4);
            return new UnityEngine.Vector4(parts[0], parts[1], parts[2], parts[3]);
        }

        private static UnityEngine.Vector2Int ParseVector2Int(string value)
        {
            var parts = ParseIntArray(value, 2);
            return new UnityEngine.Vector2Int(parts[0], parts[1]);
        }

        private static UnityEngine.Vector3Int ParseVector3Int(string value)
        {
            var parts = ParseIntArray(value, 3);
            return new UnityEngine.Vector3Int(parts[0], parts[1], parts[2]);
        }

        private static UnityEngine.Quaternion ParseQuaternion(string value)
        {
            var parts = ParseFloatArray(value, -1);
            if (parts.Length == 3)
                return UnityEngine.Quaternion.Euler(parts[0], parts[1], parts[2]);
            if (parts.Length == 4)
                return new UnityEngine.Quaternion(parts[0], parts[1], parts[2], parts[3]);
            throw new System.ArgumentException("Quaternion requires 3 (euler) or 4 (xyzw) values");
        }

        private static UnityEngine.Color ParseColor(string value)
        {
            if (value.StartsWith("#"))
            {
                if (UnityEngine.ColorUtility.TryParseHtmlString(value, out var color))
                    return color;
            }

            var namedColor = GetNamedColor(value);
            if (namedColor.HasValue)
                return namedColor.Value;

            var parts = ParseFloatArray(value, -1);
            if (parts.Length == 3)
                return new UnityEngine.Color(parts[0], parts[1], parts[2], 1);
            if (parts.Length == 4)
                return new UnityEngine.Color(parts[0], parts[1], parts[2], parts[3]);
            throw new System.ArgumentException("Color requires 3-4 float values (0-1) or hex string (#RRGGBB)");
        }

        private static UnityEngine.Color32 ParseColor32(string value)
        {
            UnityEngine.Color color = ParseColor(value);
            return color;
        }

        private static UnityEngine.Color? GetNamedColor(string name)
        {
            switch (name.ToLower().Trim())
            {
                case "red": return UnityEngine.Color.red;
                case "green": return UnityEngine.Color.green;
                case "blue": return UnityEngine.Color.blue;
                case "white": return UnityEngine.Color.white;
                case "black": return UnityEngine.Color.black;
                case "yellow": return UnityEngine.Color.yellow;
                case "cyan": return UnityEngine.Color.cyan;
                case "magenta": return UnityEngine.Color.magenta;
                case "gray": case "grey": return UnityEngine.Color.gray;
                case "clear": return UnityEngine.Color.clear;
                default: return null;
            }
        }

        private static UnityEngine.Rect ParseRect(string value)
        {
            var parts = ParseFloatArray(value, 4);
            return new UnityEngine.Rect(parts[0], parts[1], parts[2], parts[3]);
        }

        private static UnityEngine.Bounds ParseBounds(string value)
        {
            var parts = ParseFloatArray(value, 6);
            return new UnityEngine.Bounds(
                new UnityEngine.Vector3(parts[0], parts[1], parts[2]),
                new UnityEngine.Vector3(parts[3], parts[4], parts[5]));
        }

        private static UnityEngine.LayerMask ParseLayerMask(string value)
        {
            int layer = UnityEngine.LayerMask.NameToLayer(value);
            if (layer != -1)
                return 1 << layer;
            if (int.TryParse(value, out var mask))
                return mask;
            throw new System.ArgumentException($"Invalid layer: {value}");
        }

        private static UnityEngine.AnimationCurve ParseAnimationCurve(string value)
        {
            value = value.ToLower().Trim();
            switch (value)
            {
                case "linear": return UnityEngine.AnimationCurve.Linear(0, 0, 1, 1);
                case "easein": return new UnityEngine.AnimationCurve(new UnityEngine.Keyframe(0, 0, 0, 0), new UnityEngine.Keyframe(1, 1, 2, 0));
                case "easeout": return new UnityEngine.AnimationCurve(new UnityEngine.Keyframe(0, 0, 0, 2), new UnityEngine.Keyframe(1, 1, 0, 0));
                case "easeinout": return UnityEngine.AnimationCurve.EaseInOut(0, 0, 1, 1);
                case "constant": return UnityEngine.AnimationCurve.Constant(0, 1, 1);
                default: return UnityEngine.AnimationCurve.Linear(0, 0, 1, 1);
            }
        }

        private static float[] ParseFloatArray(string value, int expectedCount)
        {
            value = value.Trim('(', ')', '[', ']', '{', '}');
            var parts = value.Split(new[] { ',', ' ', ';' }, System.StringSplitOptions.RemoveEmptyEntries);

            if (expectedCount > 0 && parts.Length != expectedCount)
                throw new System.ArgumentException($"Expected {expectedCount} values, got {parts.Length}");

            return System.Linq.Enumerable.ToArray(
                System.Linq.Enumerable.Select(parts,
                    p => float.Parse(p.Trim(), System.Globalization.CultureInfo.InvariantCulture)));
        }

        private static int[] ParseIntArray(string value, int expectedCount)
        {
            value = value.Trim('(', ')', '[', ']', '{', '}');
            var parts = value.Split(new[] { ',', ' ', ';' }, System.StringSplitOptions.RemoveEmptyEntries);

            if (expectedCount > 0 && parts.Length != expectedCount)
                throw new System.ArgumentException($"Expected {expectedCount} values, got {parts.Length}");

            return System.Linq.Enumerable.ToArray(
                System.Linq.Enumerable.Select(parts,
                    p => int.Parse(p.Trim())));
        }
    }
```
