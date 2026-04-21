# Validate Helper

Paste-in parameter validation. Every method returns `null` on success or an
error object for use with the `is object err` pattern.

## Call surface

- `Validate.Required(string value, string paramName)` — non-empty check.
- `Validate.RequiredJsonArray(string jsonArray, string paramName)` — raw JSON string must be non-empty and not `"[]"` / `"null"`.
- `Validate.InRange(int value, int min, int max, string paramName)` — inclusive.
- `Validate.InRange(float value, float min, float max, string paramName)` — inclusive.
- `Validate.SafePath(string path, string paramName, bool isDelete = false)` — restricts to `Assets/` or `Packages/`, rejects `..` traversal.

## Call pattern

```csharp
if (Validate.Required(componentType, "componentType") is object err) { result.SetResult(err); return; }
if (Validate.SafePath(assetPath, "assetPath") is object pathErr) { result.SetResult(pathErr); return; }
if (Validate.InRange(index, 0, list.Count - 1, "index") is object rangeErr) { result.SetResult(rangeErr); return; }
```

## Paste-in

```csharp
    public static class Validate
    {
        public static object Required(string value, string paramName) =>
            string.IsNullOrEmpty(value) ? (object)new { error = $"{paramName} is required" } : null;

        public static object RequiredJsonArray(string jsonArray, string paramName)
        {
            if (string.IsNullOrEmpty(jsonArray))
                return new { error = $"{paramName} is required" };
            var trimmed = jsonArray.Trim();
            if (trimmed == "[]" || trimmed == "null")
                return new { error = $"{paramName} must be a non-empty array" };
            return null;
        }

        public static object InRange(float value, float min, float max, string paramName)
        {
            if (value < min || value > max)
                return new { error = $"{paramName} must be between {min} and {max}, got {value}" };
            return null;
        }

        public static object InRange(int value, int min, int max, string paramName)
        {
            if (value < min || value > max)
                return new { error = $"{paramName} must be between {min} and {max}, got {value}" };
            return null;
        }

        public static object SafePath(string path, string paramName, bool isDelete = false)
        {
            if (string.IsNullOrEmpty(path))
                return new { error = $"{paramName} is required" };

            var normalized = path.Replace('\\', '/');
            while (normalized.Contains("//")) normalized = normalized.Replace("//", "/");
            if (normalized.StartsWith("./")) normalized = normalized.Substring(2);

            if (normalized.Contains(".."))
                return new { error = $"Path traversal not allowed: {path}" };

            if (!normalized.StartsWith("Assets/") && !normalized.StartsWith("Packages/") &&
                normalized != "Assets" && normalized != "Packages")
                return new { error = $"Path must start with Assets/ or Packages/: {path}" };

            if (isDelete && (normalized == "Assets" || normalized == "Assets/" ||
                             normalized == "Packages" || normalized == "Packages/"))
                return new { error = "Cannot delete root Assets or Packages folder" };

            return null;
        }
    }
```
