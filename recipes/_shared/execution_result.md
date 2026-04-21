# ExecutionResult.SetResult Extension

Paste-in adapter that lets a recipe return a structured object via
`result.SetResult(new { ... })`. Serializes the payload to JSON and forwards
to `ExecutionResult.Log(...)`.

## Call pattern

```csharp
result.SetResult(new { success = true, objects = found });
```

## Do not

- Do not use `Newtonsoft.Json.*` in recipes — not available in `Unity_RunCommand`.
- Do not use `JsonUtility.ToJson` for anonymous types — silently returns `"{}"`.

## Paste-in

```csharp
    internal static class ExecutionResultExtensions
    {
        public static void SetResult(this ExecutionResult r, object payload)
        {
            r.Log(MiniJson.Serialize(payload));
        }
    }

    internal static class MiniJson
    {
        public static string Serialize(object value)
        {
            var sb = new System.Text.StringBuilder(256);
            Append(sb, value);
            return sb.ToString();
        }

        private static void Append(System.Text.StringBuilder sb, object value)
        {
            if (value == null) { sb.Append("null"); return; }
            if (value is string s) { AppendString(sb, s); return; }
            if (value is bool bv) { sb.Append(bv ? "true" : "false"); return; }
            if (value is System.Enum ev) { AppendString(sb, ev.ToString()); return; }

            var t = value.GetType();
            if (t.IsPrimitive)
            {
                var ci = System.Globalization.CultureInfo.InvariantCulture;
                if (value is float fv) sb.Append(fv.ToString("R", ci));
                else if (value is double dv) sb.Append(dv.ToString("R", ci));
                else sb.Append(System.Convert.ToString(value, ci));
                return;
            }
            if (value is decimal dec) { sb.Append(dec.ToString(System.Globalization.CultureInfo.InvariantCulture)); return; }

            if (value is System.Collections.IDictionary dict)
            {
                sb.Append('{');
                bool first = true;
                foreach (System.Collections.DictionaryEntry entry in dict)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    AppendString(sb, entry.Key != null ? entry.Key.ToString() : "null");
                    sb.Append(':');
                    Append(sb, entry.Value);
                }
                sb.Append('}');
                return;
            }

            if (value is UnityEngine.Object uo)
            {
                AppendString(sb, uo != null ? uo.name : "null");
                return;
            }

            if (value is System.Collections.IEnumerable en)
            {
                sb.Append('[');
                bool first = true;
                foreach (var item in en)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    Append(sb, item);
                }
                sb.Append(']');
                return;
            }

            sb.Append('{');
            bool firstMember = true;
            foreach (var prop in t.GetProperties())
            {
                if (prop.GetIndexParameters().Length != 0) continue;
                var getter = prop.GetGetMethod();
                if (getter == null || getter.IsStatic) continue;
                object pv;
                try { pv = prop.GetValue(value, null); } catch { continue; }
                if (!firstMember) sb.Append(',');
                firstMember = false;
                AppendString(sb, prop.Name);
                sb.Append(':');
                Append(sb, pv);
            }
            foreach (var fld in t.GetFields())
            {
                if (fld.IsStatic) continue;
                object fv;
                try { fv = fld.GetValue(value); } catch { continue; }
                if (!firstMember) sb.Append(',');
                firstMember = false;
                AppendString(sb, fld.Name);
                sb.Append(':');
                Append(sb, fv);
            }
            sb.Append('}');
        }

        private static void AppendString(System.Text.StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
```

## Notes

- Anonymous types serialize via public instance properties; plain classes/structs
  also work.
- `UnityEngine.Object` serializes as its `.name`; emit the instance id explicitly
  when needed: `new { id = obj.GetInstanceID(), ... }`.
- `float` / `double` use round-trip format (`"R"`) to preserve precision.
- Paste this class in the same `Unity_RunCommand` code block as `CommandScript`.
