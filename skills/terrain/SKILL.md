---
name: unity-terrain
description: "Use when users want to create terrain, set heights, paint textures, or add trees."
---

# Unity Terrain Skills

## Overview

> **Note**: Terrain operations require an existing Terrain in the scene, or use `terrain_create` to generate one.

## Common Mistakes


**DO NOT** (common hallucinations):
- `terrain_set_texture` does not exist → use `terrain_paint_texture` with layer index and brush parameters
- `terrain_add_tree` / `terrain_add_grass` do not exist → these require Unity Terrain tools or custom scripts
- `terrain_set_size` does not exist → terrain dimensions are set at creation via `terrain_create`
- `terrain_import_heightmap` / `terrain_set_heights` do not exist → use `terrain_set_heights_batch` with a 2D heights array (`[z][x]` values 0-1)

**Routing**:
- For terrain material → use `material` module on terrain's material
- For objects on terrain → use `gameobject` module to create/place objects

## Skills Overview

| Skill | Description |
|-------|-------------|
| `terrain_create` | Create new Terrain with TerrainData |
| `terrain_get_info` | Get terrain size, resolution, layers |
| `terrain_get_height` | Get height at world position |
| `terrain_set_height` | Set height at normalized coords |
| `terrain_set_heights_batch` | Batch set heights in region |
| `terrain_add_hill` | ⭐ Add smooth hill with radius and falloff |
| `terrain_generate_perlin` | ⭐ Generate natural terrain using Perlin noise |
| `terrain_smooth` | ⭐ Smooth terrain to reduce sharp edges |
| `terrain_flatten` | ⭐ Flatten terrain to target height |
| `terrain_paint_texture` | Paint texture layer at position |

---

## Skills

### terrain_create
Create a new Terrain GameObject with TerrainData asset.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `name` | string | No | "Terrain" | Terrain name |
| `width` | int | No | 500 | Terrain width (X) |
| `length` | int | No | 500 | Terrain length (Z) |
| `height` | int | No | 100 | Max terrain height (Y) |
| `heightmapResolution` | int | No | 513 | Heightmap resolution (power of 2 + 1) |
| `x`, `y`, `z` | float | No | 0 | Position |

**Returns**: `{success, name, instanceId, terrainDataPath, size, position}`

### terrain_get_info
Get terrain information.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | No* | Terrain name |
| `instanceId` | int | No* | Instance ID |

*If neither provided, uses first terrain in scene

**Returns**: `{success, name, size, heightmapResolution, alphamapResolution, terrainLayerCount, layers}`

### terrain_get_height
Get terrain height at world position.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `worldX` | float | Yes | World X coordinate |
| `worldZ` | float | Yes | World Z coordinate |
| `name` | string | No | Terrain name |

**Returns**: `{success, worldX, worldZ, height, worldY}`

### terrain_set_height
Set height at normalized coordinates.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `normalizedX` | float | Yes | X position (0-1) |
| `normalizedZ` | float | Yes | Z position (0-1) |
| `height` | float | Yes | Height value (0-1) |
| `name` | string | No | Terrain name |

**Returns**: `{success, normalizedX, normalizedZ, height, pixelX, pixelZ}`

### terrain_set_heights_batch
⚠️ **BATCH SKILL**: Set heights in rectangular region.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `startX` | int | Yes | Start X pixel index |
| `startZ` | int | Yes | Start Z pixel index |
| `heights` | float[][] | Yes | 2D array [z][x] with values 0-1 |
| `name` | string | No | Terrain name |

**Returns**: `{success, startX, startZ, modifiedWidth, modifiedLength, totalPointsModified}`

*See `../../recipes/terrain/<command>.md` for C# templates.*

### terrain_add_hill
⭐ **RECOMMENDED**: Add a smooth, natural-looking hill to the terrain.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `normalizedX` | float | Yes | - | X position (0-1) |
| `normalizedZ` | float | Yes | - | Z position (0-1) |
| `radius` | float | No | 0.2 | Hill radius (0-1, relative to terrain size) |
| `height` | float | No | 0.5 | Hill height (0-1) |
| `smoothness` | float | No | 1.0 | Smoothness factor (higher = smoother) |
| `name` | string | No | null | Terrain name |

**Returns**: `{success, centerX, centerZ, radius, height, affectedArea}`

*See `../../recipes/terrain/<command>.md` for C# templates.*

### terrain_generate_perlin
⭐ **RECOMMENDED**: Generate natural-looking terrain using Perlin noise algorithm.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `scale` | float | No | 20.0 | Noise scale (lower = larger features) |
| `heightMultiplier` | float | No | 0.3 | Height intensity (0-1) |
| `octaves` | int | No | 4 | Detail layers (more = more detail) |
| `persistence` | float | No | 0.5 | Amplitude decrease per octave |
| `lacunarity` | float | No | 2.0 | Frequency increase per octave |
| `seed` | int | No | 0 | Random seed (0 = random) |
| `name` | string | No | null | Terrain name |

**Returns**: `{success, resolution, scale, heightMultiplier, octaves, persistence, lacunarity, seed}`

*See `../../recipes/terrain/<command>.md` for C# templates.*

### terrain_smooth
⭐ Smooth terrain heights to reduce sharp edges and create natural transitions.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `normalizedX` | float | Yes | - | X position (0-1) |
| `normalizedZ` | float | Yes | - | Z position (0-1) |
| `radius` | float | No | 0.1 | Smoothing radius (0-1) |
| `iterations` | int | No | 1 | Number of smoothing passes |
| `name` | string | No | null | Terrain name |

**Returns**: `{success, centerX, centerZ, radius, iterations, affectedArea}`

*See `../../recipes/terrain/<command>.md` for C# templates.*

### terrain_flatten
⭐ Flatten terrain to a specific height in a region.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `normalizedX` | float | Yes | - | X position (0-1) |
| `normalizedZ` | float | Yes | - | Z position (0-1) |
| `targetHeight` | float | No | 0.5 | Target height (0-1) |
| `radius` | float | No | 0.1 | Flatten radius (0-1) |
| `strength` | float | No | 1.0 | Flatten strength (0-1) |
| `name` | string | No | null | Terrain name |

**Returns**: `{success, centerX, centerZ, targetHeight, radius, strength}`

*See `../../recipes/terrain/<command>.md` for C# templates.*

### terrain_paint_texture
Paint terrain texture layer. Requires terrain layers already configured.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `normalizedX` | float | Yes | - | X position (0-1) |
| `normalizedZ` | float | Yes | - | Z position (0-1) |
| `layerIndex` | int | Yes | - | Terrain layer index (0-based; use `terrain_get_info` to query available layers) |
| `strength` | float | No | 1.0 | Paint strength |
| `brushSize` | int | No | 10 | Brush size in pixels |
| `name` | string | No | null | Terrain name |

**Returns**: `{success, layerIndex, layerName, centerX, centerZ}`

---

## Example Usage

*See `../../recipes/terrain/<command>.md` for C# templates.*

Recipe path rule: `../../recipes/terrain/<command>.md`

---
