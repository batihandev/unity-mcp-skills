---
name: unity-importer
description: "Use when users want to configure texture, audio, or model import settings."
---

# Unity Importer Skills

## When to Use

Use this module to change import **settings** for textures, audio, and models that already exist in the project. For bringing new files into the project use `asset_import` in the `asset` module.

> **Batch-first**: Prefer the batch setters when configuring `2+` assets of the same category.

## Common Mistakes

**DO NOT** (common hallucinations):
- `importer_import` does not exist → use `asset_import` in the `asset` module to bring files into the project
- `importer_set_format` does not exist → use the specific texture/audio/model setters
- `importer_get_settings` does not exist → use the category-specific getters
- Settings changes do not always apply instantly in memory. Reimport may still be required

**Routing**:
- File import or refresh → `asset`
- Texture settings → `texture_*`
- Audio settings → `audio_*`
- Model settings → `model_*`
- Alternative importer bridge skills → `texture_set_import_settings`, `audio_set_import_settings`, `model_set_import_settings`
- Force importer refresh → `asset_reimport` or `asset_reimport_batch`

## Quick Reference

### Texture Route

Import settings:

| Skill | Use | Key parameters |
|-------|-----|----------------|
| `texture_get_settings` | Read texture importer settings | `assetPath` |
| `texture_set_settings` | Set texture importer settings | `assetPath`, `textureType?`, `maxSize?`, `filterMode?`, `compression?`, `mipmapEnabled?`, `sRGB?`, `readable?`, `wrapMode?` |
| `texture_set_settings_batch` | Batch texture settings | `items` |
| `texture_get_import_settings` | Read minimal importer settings (type/maxSize/compression/filter/srgb/readable/mipmap) | `assetPath` |
| `texture_set_import_settings` | Alternative texture import bridge | similar texture fields |

Query and runtime info:

| Skill | Use | Key parameters |
|-------|-----|----------------|
| `texture_find_assets` | Search Texture2D assets by AssetDatabase filter | `filter?`, `limit?` (default 50) |
| `texture_get_info` | Inspect dimensions, format, and runtime memory size | `assetPath` |
| `texture_find_by_size` | Find textures in a dimension range (pixels) | `minSize?` (0), `maxSize?` (99999), `limit?` (50) |

Typed / platform overrides:

| Skill | Use | Key parameters |
|-------|-----|----------------|
| `texture_set_type` | Switch texture type | `assetPath`, `textureType` (`Default`/`NormalMap`/`Sprite`/`EditorGUI`/`Cursor`/`Cookie`/`Lightmap`/`SingleChannel`) |
| `texture_set_platform_settings` | Override per-platform settings | `assetPath`, `platform` (`Standalone`/`iPhone`/`Android`/`WebGL`), `maxSize?`, `format?`, `compressionQuality?`, `overridden?` |
| `texture_get_platform_settings` | Read per-platform override | `assetPath`, `platform` |
| `texture_set_sprite_settings` | Sprite-specific knobs (PPU, mode) | `assetPath`, `pixelsPerUnit?`, `spriteMode?` (`Single`/`Multiple`/`Polygon`) |
| `sprite_set_import_settings` | Sprite importer bridge (PPU, packingTag, pivot) | `assetPath`, `spriteMode?`, `pixelsPerUnit?`, `packingTag?`, `pivotX?`, `pivotY?` |

**Texture core fields**:

| Field | Values |
|-------|--------|
| `textureType` | `Default`, `NormalMap`, `Sprite`, `EditorGUI`, `Cursor`, `Cookie`, `Lightmap`, `SingleChannel` |
| `maxSize` | `32`–`8192` |
| `filterMode` | `Point`, `Bilinear`, `Trilinear` |
| `compression` | `None`, `LowQuality`, `NormalQuality`, `HighQuality` |
| `wrapMode` | `Repeat`, `Clamp`, `Mirror`, `MirrorOnce` |

**Platform override fields**: `platform` (`Standalone`/`iPhone`/`Android`/`WebGL`), `maxSize`, `format`, `compressionQuality` (0–100), `overridden`

Common texture decisions:
- UI sprites → `textureType="Sprite"`, usually `mipmapEnabled=false`
- Pixel art → `filterMode="Point"`
- Runtime CPU reads → `readable=true` only when necessary
- Platform-tuned builds → prefer `texture_set_platform_settings` over global `texture_set_settings`

### Audio Route

Import settings:

| Skill | Use | Key parameters |
|-------|-----|----------------|
| `audio_get_settings` | Read audio importer settings | `assetPath` |
| `audio_set_settings` | Set audio importer settings | `assetPath`, `forceToMono?`, `loadInBackground?`, `loadType?`, `compressionFormat?`, `quality?` |
| `audio_set_settings_batch` | Batch audio settings | `items` |
| `audio_get_import_settings` | Read minimal importer defaults (loadType/format/quality/forceToMono/loadInBackground) | `assetPath` |
| `audio_set_import_settings` | Alternative audio import bridge | similar audio fields |

Clip query and info:

| Skill | Use | Key parameters |
|-------|-----|----------------|
| `audio_find_clips` | Search `AudioClip` assets by filter | `filter?`, `limit?` (default 50) |
| `audio_get_clip_info` | Inspect length/channels/frequency/samples of a clip | `assetPath` |

Scene runtime (`AudioSource` / `AudioMixer`):

| Skill | Use | Key parameters |
|-------|-----|----------------|
| `audio_add_source` | Add an `AudioSource` to a GameObject | target (`name`/`instanceId`/`path`), `clipPath?`, `playOnAwake?` (false), `loop?` (false), `volume?` (1) |
| `audio_get_source_info` | Read the AudioSource configuration | target |
| `audio_set_source_properties` | Update AudioSource fields | target, `clipPath?`, `volume?`, `pitch?`, `loop?`, `playOnAwake?`, `mute?`, `spatialBlend?`, `priority?` |
| `audio_find_sources_in_scene` | List all AudioSources in the active scene | `limit?` (default 50) |
| `audio_create_mixer` | Create a new `AudioMixer` asset | `mixerName?` (default `NewAudioMixer`), `folder?` (default `Assets`) |

**Audio core fields**:

| Field | Values |
|-------|--------|
| `loadType` | `DecompressOnLoad`, `CompressedInMemory`, `Streaming` |
| `compressionFormat` | `PCM`, `Vorbis`, `ADPCM` |
| `quality` | `0`–`100` integer (`audio_set_import_settings` bridge); mapped to `0.0`–`1.0` internally |
| `forceToMono` | Convert to mono |
| `loadInBackground` | Background load |

**Practical presets**:

| Asset type | Suggested settings |
|------------|--------------------|
| BGM | `Streaming` + `Vorbis` + medium quality |
| Short SFX | `DecompressOnLoad` |
| Large SFX bank | `CompressedInMemory` or mono where acceptable |

Common audio decisions:
- Long BGM → `loadType="Streaming"`
- Short SFX → `loadType="DecompressOnLoad"`
- Memory-sensitive SFX libraries → consider `forceToMono=true`
- Scene-side AudioSource tuning → prefer `audio_set_source_properties` over manual component edits

### Model Route

Import settings:

| Skill | Use | Key parameters |
|-------|-----|----------------|
| `model_get_settings` | Read model importer settings | `assetPath` |
| `model_set_settings` | Set model importer settings | `assetPath`, `globalScale?`, `meshCompression?`, `isReadable?`, `generateSecondaryUV?`, `animationType?`, `importAnimation?`, `importCameras?`, `importLights?`, `materialImportMode?` |
| `model_set_settings_batch` | Batch model settings | `items` |
| `model_get_import_settings` | Read minimal importer defaults (scale/compression/animationType/importAnimation/materialImportMode) | `assetPath` |
| `model_set_import_settings` | Alternative model import bridge | similar model fields |

Query and info:

| Skill | Use | Key parameters |
|-------|-----|----------------|
| `model_find_assets` | Search model assets by filter | `filter?`, `limit?` (default 50) |
| `model_get_mesh_info` | Mesh vertex / triangle / submesh stats | target (`name`/`instanceId`/`path`) or `assetPath` |
| `model_get_materials_info` | Inspect sub-asset materials embedded in the model | `assetPath` |
| `model_get_animations_info` | List animation clips and framerates on the model | `assetPath` |
| `model_get_rig_info` | Read animationType, avatar, skeleton binding info | `assetPath` |

Animation and rig:

| Skill | Use | Key parameters |
|-------|-----|----------------|
| `model_set_animation_clips` | Configure animation clip splits | `assetPath`, `clips` (JSON array of `{name, firstFrame, lastFrame, loop}`) |
| `model_set_rig` | Switch rig/skeleton mode | `assetPath`, `animationType` (`None`/`Legacy`/`Generic`/`Humanoid`), `avatarSetup?` |

**Model core fields**:

| Field | Values |
|-------|--------|
| `globalScale` | Import scale factor |
| `meshCompression` | `Off`, `Low`, `Medium`, `High` |
| `animationType` | `None`, `Legacy`, `Generic`, `Humanoid` |
| `materialImportMode` | `model_set_settings` string field for material import mode |
| `importMaterials` | `model_set_import_settings` bridge: bool shorthand (true = import via material description, false = None) |
| `isReadable` | CPU-readable mesh |
| `generateSecondaryUV` | Generate lightmap UVs |

**Practical presets**:

| Asset type | Suggested settings |
|------------|--------------------|
| Humanoid character | `animationType="Humanoid"` |
| Static prop | disable cameras/lights/animation, compress mesh |
| Baked environment mesh | enable secondary UVs when needed |

Common model decisions:
- Characters → `animationType="Humanoid"` when retargeting is required
- Static props → disable cameras/lights/animation imports when unused
- Baked-lighting meshes → enable secondary UVs when appropriate
- After `model_set_rig` or `model_set_animation_clips` → call `asset_reimport` to refresh clips and avatar

## Reimport Rule

After importer changes, use reimport when you need Unity to fully refresh the asset:

| Skill | Use |
|-------|-----|
| `asset_reimport` | Reimport one asset |
| `asset_reimport_batch` | Reimport assets matching a search scope |

## Best Practices

1. Batch assets by category and apply one policy per batch.
2. Only enable `readable` when runtime CPU access is necessary.
3. Reimport deliberately after changing importer-critical fields.
4. Keep character rig settings and clip splits documented, because they are easy to drift.
5. Use platform overrides only when there is a real target-platform constraint.

Recipe path rule: `../../recipes/importer/<command>.md`

