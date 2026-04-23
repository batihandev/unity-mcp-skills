# shader_create

Create a new shader file at the given path. If `template` is omitted, a default Unlit CG shader is generated using `shaderName`.

**Signature:** `ShaderCreate(string shaderName, string savePath, string template = null)`

**Returns:** `{ success, shaderName, path }`

## Notes
- `shaderName` is the internal shader name used in the `Shader "..."` declaration (e.g., `"Custom/MyShader"`).
- `savePath` must be an `Assets/`-rooted path ending in `.shader`.
- If `template` is provided, its content is written verbatim; otherwise a full CG Unlit shader is generated.
- The asset is imported and snapshot-tracked after creation.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md), [`skills_common`](../_shared/skills_common.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string shaderName = "Custom/MyShader";
        string savePath = "Assets/Shaders/MyShader.shader";
        string template = null; // null → generate default Unlit CG shader

                    if (Validate.Required(shaderName, "shaderName") is object err) { result.SetResult(err); return; }
                    if (!string.IsNullOrEmpty(savePath) && Validate.SafePath(savePath, "savePath") is object pathErr) { result.SetResult(pathErr); return; }

                    if (!string.IsNullOrEmpty(savePath) && File.Exists(savePath))
                        { result.SetResult(new { error = $"File already exists: {savePath}" }); return; }

                    var dir = Path.GetDirectoryName(savePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    var content = template ?? $@"Shader ""{shaderName}""
        {{
            Properties
            {{
                _MainTex (""Texture"", 2D) = ""white"" {{}}
                _Color (""Color"", Color) = (1,1,1,1)
            }}
            SubShader
            {{
                Tags {{ ""RenderType""=""Opaque"" }}
                LOD 100

                Pass
                {{
                    CGPROGRAM
                    #pragma vertex vert
                    #pragma fragment frag
                    #include ""UnityCG.cginc""

                    struct appdata
                    {{
                        float4 vertex : POSITION;
                        float2 uv : TEXCOORD0;
                    }};

                    struct v2f
                    {{
                        float2 uv : TEXCOORD0;
                        float4 vertex : SV_POSITION;
                    }};

                    sampler2D _MainTex;
                    float4 _MainTex_ST;
                    fixed4 _Color;

                    v2f vert (appdata v)
                    {{
                        v2f o;
                        o.vertex = UnityObjectToClipPos(v.vertex);
                        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                        return o;
                    }}

                    fixed4 frag (v2f i) : SV_Target
                    {{
                        fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                        return col;
                    }}
                    ENDCG
                }}
                    }}
        }}
        ";
                    File.WriteAllText(savePath, content, SkillsCommon.Utf8NoBom);
                    AssetDatabase.ImportAsset(savePath);

                    var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(savePath);
                    if (asset != null) WorkflowManager.SnapshotCreatedAsset(asset);

                    { result.SetResult(new { success = true, shaderName, path = savePath }); return; }
    }
}
```
