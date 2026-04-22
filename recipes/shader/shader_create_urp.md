# shader_create_urp

Create a URP shader from template (Unlit or Lit).

**Signature:** `ShaderCreateUrp(string shaderName, string savePath, string type = "Unlit")`

**Returns:** `{ success, shaderName, savePath, type }`

**Notes:**
- `type`: `"Unlit"` or `"Lit"` — Lit includes lighting and shadow support
- `savePath` must end in `.shader` and the parent directory must exist (or will be created)
- Fails if the file already exists

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md), [`skills_common`](../_shared/skills_common.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string shaderName = "MyShader";
        string savePath = "Assets/Shaders/MyShader.shader";
        string type = "Unlit"; // "Unlit" or "Lit"

        if (Validate.SafePath(savePath, "savePath") is object pathErr) { result.SetResult(pathErr); return; }
        if (File.Exists(savePath)) { result.SetResult(new { error = $"File already exists: {savePath}" }); return; }
        var dir = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string content = type.ToLower() == "lit"
            ? $@"Shader ""{shaderName}""
{{
    Properties
    {{
        _BaseMap (""Base Map"", 2D) = ""white"" {{}}
        _BaseColor (""Base Color"", Color) = (1,1,1,1)
    }}
    SubShader
    {{
        Tags {{ ""RenderType""=""Opaque"" ""RenderPipeline""=""UniversalPipeline"" }}
        Pass
        {{
            Name ""ForwardLit""
            Tags {{ ""LightMode""=""UniversalForward"" }}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl""
            #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl""
            struct Attributes {{ float4 positionOS : POSITION; float2 uv : TEXCOORD0; float3 normalOS : NORMAL; }};
            struct Varyings {{ float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; }};
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial) float4 _BaseMap_ST; half4 _BaseColor; CBUFFER_END
            Varyings vert(Attributes IN) {{ Varyings OUT; OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz); OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap); OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS); return OUT; }}
            half4 frag(Varyings IN) : SV_Target {{ half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor; return col; }}
            ENDHLSL
        }}
    }}
}}"
            : $@"Shader ""{shaderName}""
{{
    Properties
    {{
        _BaseMap (""Base Map"", 2D) = ""white"" {{}}
        _BaseColor (""Base Color"", Color) = (1,1,1,1)
    }}
    SubShader
    {{
        Tags {{ ""RenderType""=""Opaque"" ""RenderPipeline""=""UniversalPipeline"" }}
        Pass
        {{
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl""
            struct Attributes {{ float4 positionOS : POSITION; float2 uv : TEXCOORD0; }};
            struct Varyings {{ float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; }};
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial) float4 _BaseMap_ST; half4 _BaseColor; CBUFFER_END
            Varyings vert(Attributes IN) {{ Varyings OUT; OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz); OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap); return OUT; }}
            half4 frag(Varyings IN) : SV_Target {{ return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor; }}
            ENDHLSL
        }}
    }}
}}";

        File.WriteAllText(savePath, content, SkillsCommon.Utf8NoBom);
        AssetDatabase.ImportAsset(savePath);

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(savePath);
        if (asset != null) WorkflowManager.SnapshotCreatedAsset(asset);
        result.SetResult(new { success = true, shaderName, path = savePath, type });
    }
}
```
