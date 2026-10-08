# Material operations

Read [foundation routing and safety](foundation.md) first. Material work has two distinct targets. A `.mat` asset is one shared persistent asset: editing it affects every renderer that references it. A scene target is one exact `Renderer` handle plus one zero-based material slot: editing that slot changes the scene reference, while editing the material obtained from that slot changes its shared material. Do not select a renderer, material, or slot from a display name. Enumerate candidates, select exact handles, and read the selected state before and after a mutation.

Use `get_material_properties` for a selected material's shader, queue, keywords, and current property values; its keyword list also serves keyword-only reads. Use `get_shader_properties` with exactly one of `shader` or `material` to discover the declared name and type before setting a property. `list_shaders` accepts `filter`, `includeBuiltin`, and `limit` (default 200) for shader discovery. A shader name is usable only after discovery shows a supported entry.

Pipeline defaults are candidates that require shader discovery and live readback. Built-in candidates are `Standard`, `_Color`, and `_MainTex`; URP candidates are `Universal Render Pipeline/Lit`, `_BaseColor`, and `_BaseMap`; HDRP candidates are `HDRP/Lit`, `_BaseColor`, and `_BaseColorMap`. Confirm the active pipeline, supported shader, and declared `Color` or `TexEnv` property before accepting an omitted property. If no appropriate property is present, refuse without writing. The catalog's omitted-shader behavior chooses `Universal Render Pipeline/Lit` for any Scriptable Render Pipeline, falling back to `Standard`; this does not select the HDRP candidate and must not be used as an HDRP default.

## Create, copy, inspect, and assign

For persistent creation, use `create_asset(path,type,shader?,confirm?,dry_run?)`, with `type="UnityEngine.Material"` and a confined `.mat` path. A supplied folder resolves to `<folder>/<name>.mat`; a supplied full `.mat` path is used directly. Resolve an omitted shader from the active pipeline and verify it first; an explicitly requested unavailable shader refuses. With no save destination, use the memory-only evaluation below. `confirm=true` is only the catalog guard for replacing an existing path. Creation and copying are asset writes: treat them as non-Undo operations and clean up only assets created for the operation.

Copy a selected material asset with `copy_asset(asset,destination,confirm?,dry_run?)`. Require that `asset` resolves to a Material and that `destination` is a confined `.mat` path. When the destination is omitted, use the source folder and requested new name plus `.mat`; an explicit folder uses that same filename rule. The copy receives a fresh GUID. Verify shader, properties, keywords, and queue on the destination; do not overwrite without the owner's `confirm=true` guard.

To assign a material, select one exact `Renderer` component and an in-range slot. Read `m_Materials`, then set only `m_Materials.Array.data[slot]` through `set_serialized_field(target,field,value,component?)`, passing the selected material asset reference as `value`. Read that same slot afterward. Do not resize the array or infer a renderer from a GameObject with several renderers.

Creation and assignment batches retain input order and return one result per requested item. For asset creation use nontransactional continuation. Renderer assignment may use a native batch only when its owner supports the requested undo/transaction boundary; otherwise retain successful scene changes for explicit restoration. For a creation list mixing asset destinations and memory-only items, invoke the appropriate owner in original request order and retain each outcome; evaluation commands cannot be placed inside native `batch`. A failure does not authorize changing later item selection.

### Memory-only creation

Omitting a save destination creates a temporary material, with no asset path or disk write. Supply one name/shader pair for a single material or matching arrays for ordered continuation after individual failures. Retain returned handles for assignment or explicit `UnityEngine.Object.DestroyImmediate` cleanup. These objects are lost when the Editor exits; this workflow has no transactional or Undo claim.

```csharp
string[] names = { "TemporaryMaterial" };
string[] shaders = { "REPLACE_WITH_DISCOVERED_SHADER" };
if (names == null || shaders == null || names.Length == 0 || names.Length != shaders.Length)
    throw new System.ArgumentException("Supply matching nonempty name and shader arrays");
var results = new System.Collections.Generic.List<object>();
int successCount = 0;
for (int i = 0; i < names.Length; i++) {
    try {
        if (string.IsNullOrWhiteSpace(names[i])) throw new System.ArgumentException("Material name is required");
        var shader = UnityEngine.Shader.Find(shaders[i]);
        if (shader == null || !shader.isSupported) throw new System.ArgumentException("Select an available supported shader");
        var material = new UnityEngine.Material(shader) { name = names[i] };
        results.Add(new { success = true, name = material.name, shader = shader.name, path = (string)null,
            handle = UnityEngine.EntityId.ToULong(material.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture) });
        successCount++;
    } catch (System.Exception error) {
        results.Add(new { success = false, name = names[i], error = error.Message });
    }
}
return new { success = successCount == names.Length, totalItems = names.Length, successCount,
    failCount = names.Length - successCount, results };
```

## Properties, shader, queue, and keywords

`set_material_properties(material,shader?,properties?,renderQueue?,enableKeywords?,disableKeywords?,confirm?,dry_run?)` is the native owner for the following operations on one selected material:

- Set a discovered `Color` property using `[r,g,b,a]` or the supported hex representation. Omitted alpha is `1`.
- Set a discovered `Float` or `Range` property with a number.
- Set a discovered `Int` property with a number.
- Set a discovered `Vector` property with `[x,y,z,w]`; each omitted component defaults to `0`.
- Set a discovered `TexEnv` property to an exact texture object reference, or `null` to clear it.
- Supply `shader` to change shaders; it is applied before properties. Read the new property inventory because a shader change can alter available or retained values.
- Supply `renderQueue`; its default `-1` inherits the shader queue. Other queue categories are Background below 2000, Geometry 2000–2449, AlphaTest 2450–2499, GeometryLast 2500–2999, Transparent 3000–3999, and Overlay from 4000.
- Use `enableKeywords` or `disableKeywords` for a keyword declared by the selected shader. Read its public `shader.keywordSpace.keywords` names first and refuse unknown names; a valid keyword does not need a matching shader property. Verify the resulting enabled keyword set because a native applied message alone can report a name with no effect.

`dry_run=true` validates without writing. Invalid properties or types can produce a CLI failure instead of a successful preview object. Check the actual response and keep the material unchanged on any refusal; preflight all requested names and declared types before a multi-property mutation. The catalog's `confirm` field is reserved for parity and is not a confirmation gate for an existing-material edit.

For emission, inputs `r`, `g`, and `b` default to `1`; `intensity` defaults to `1`; alpha is always `1`. Emission is enabled by default. Select the path from the actual shader's declared properties and keywords:

- Built-in Standard and URP Lit use `_EmissionColor`; a custom shader may expose `_Emission`. Require a declared `Color` property and `_EMISSION` keyword. Write `[r * intensity, g * intensity, b * intensity, 1]`; enable `_EMISSION` only when enabled and intensity is greater than zero, and disable it otherwise.
- HDRP Lit uses `_EmissiveColor` and has no `_EMISSION` toggle. Write the scaled color when enabled and intensity is positive; write `[0,0,0,1]` when disabled or intensity is nonpositive. `_EmissiveColorMap` and its declared `_EMISSIVE_COLOR_MAP` keyword control the emission map, independently of the color. An assigned map does not authorize an undeclared `_EMISSION` keyword.

Set global illumination to `RealtimeEmissive` for positive enabled emission and `EmissiveIsBlack` otherwise through the GI evaluation below. Read back the selected color, declared keyword state, and GI flags together. Refuse an unsupported shader layout before mutation.

Color and emission batches use ordered continuation; asset-backed batches are nontransactional, while any scene rollback claim requires support from the selected owner.

## Texture scale and offset

Texture scale and offset use serialized material state because the native material-property command does not expose texture transform fields. First discover the requested `TexEnv` property from `get_shader_properties`, then inspect `m_SavedProperties.m_TexEnvs` on the exact material. Locate the entry whose `.first` exactly matches the property name and immediately revalidate that key. Set the selected entry's `.second.m_Scale` or `.second.m_Offset` with a named Vector2 value `{ "x": number, "y": number }` through `set_serialized_field`.

The TexEnv array index is transient discovery output. Never cache it, supply it as user input, or guess it. The defaults are scale `{x:1,y:1}` and offset `{x:0,y:0}`. Missing entries and non-`TexEnv` properties refuse unchanged.

## Global-illumination flags

Resolve the material from the selected asset or renderer slot, then supply its exact decimal handle to this public-API evaluation. It accepts the five named values case-insensitively, including `AnyEmissive` (combined realtime and baked flags), and rejects invalid text before mutation. It records Undo for the loaded material and marks it dirty. Saving an asset is a separate persistence step; after an Undo of a saved change, save the restored value if disk restoration is required.

```csharp
string materialHandle = "REPLACE_WITH_MATERIAL_DECIMAL_ID";
string requestedFlags = "RealtimeEmissive";
if (!ulong.TryParse(materialHandle, System.Globalization.NumberStyles.None,
        System.Globalization.CultureInfo.InvariantCulture, out var parsedHandle))
    throw new System.ArgumentException("materialHandle must be an unsigned decimal EntityId string");
var material = UnityEditor.EditorUtility.EntityIdToObject(
    UnityEngine.EntityId.FromULong(parsedHandle)) as UnityEngine.Material;
if (material == null)
    throw new System.ArgumentException("materialHandle is stale or does not resolve to a Material");
var validName = string.Equals(requestedFlags, "None", System.StringComparison.OrdinalIgnoreCase)
    || string.Equals(requestedFlags, "RealtimeEmissive", System.StringComparison.OrdinalIgnoreCase)
    || string.Equals(requestedFlags, "BakedEmissive", System.StringComparison.OrdinalIgnoreCase)
    || string.Equals(requestedFlags, "EmissiveIsBlack", System.StringComparison.OrdinalIgnoreCase)
    || string.Equals(requestedFlags, "AnyEmissive", System.StringComparison.OrdinalIgnoreCase);
if (!validName || !System.Enum.TryParse<UnityEngine.MaterialGlobalIlluminationFlags>(requestedFlags, true, out var parsedFlags))
    throw new System.ArgumentException("flags must be None, RealtimeEmissive, BakedEmissive, EmissiveIsBlack, or AnyEmissive");
var before = material.globalIlluminationFlags;
UnityEditor.Undo.RecordObject(material, "Set material global illumination flags");
material.globalIlluminationFlags = parsedFlags;
UnityEditor.EditorUtility.SetDirty(material);
var after = material.globalIlluminationFlags;
return new {
    exactId = UnityEngine.EntityId.ToULong(material.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture),
    beforeName = before.ToString(), beforeNumericFlags = (int)before,
    afterName = after.ToString(), afterNumericFlags = (int)after
};
```

## Batch and target rules

All batches preserve request order and report success or failure for every item. Continue after independent failures only with the native continuation mode (`transactional=false`, `on_error=continue`) when the owner supports it. Asset operations and other non-Undo work are nontransactional. Do not describe a batch as transactional or Undoable without support from the exact operations it contains.

Every property operation starts with the material chosen by the target branch. A renderer branch requires both the renderer handle and slot; an asset branch requires the material asset. Shader property names, object references, and pipeline defaults are verified against that selected material immediately before mutation.
