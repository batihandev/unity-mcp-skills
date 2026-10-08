# Terrain

The optional command package owns TerrainData creation and authoring. Select an exact **Terrain component** EntityId or GlobalObjectId in a regular loaded scene. `terrain.inspect` and `terrain.sample-height` also work in Play Mode; authoring commands require Edit Mode. A TerrainData reference may be shared: every Terrain using it sees an edit. Each response reports its exact data identity, asset path, and loaded scene Terrain users. Inspect those identities before writing.

All writes take `--dryRun true` for validation and preview, or `--confirm true` to apply. Preview takes precedence. Persistent TerrainData must reside at a guarded project-relative `Assets/...` path. Transient data can be edited, but `saved` is false. Height and paint writes register Undo on their data; paint also records alphamap textures. Save/reopen and Undo should be verified on the selected project. A scene Undo of creation removes the Terrain object while leaving its TerrainData asset; delete that asset explicitly if it is no longer needed. No command creates TerrainLayers or texture assets.

| Intent | Typed command | Coordinates and defaults |
| --- | --- | --- |
| Create Terrain and data | `terrain.create` | `nameTerrain=Terrain`, size `width=500,height=100,length=500`, `resolution=513`, position `x=y=z=0`; omitted `assetPath` generates a unique `Assets/<name>_Data.asset` |
| Read dimensions, resolutions, layers, sharing | `terrain.inspect` | Exact Terrain target |
| Sample surface height | `terrain.sample-height` | World `worldX=0,worldZ=0`; Unity clamps the sample to Terrain bounds; `worldY=height+Terrain Y` |
| Set one sample | `terrain.height-set` | Normalized `normalizedX=.5,normalizedZ=.5,height=.5`; coordinates and value clamp to `0..1` and round to pixels |
| Set a rectangle | `terrain.heights-set` | Pixel `startX=0,startZ=0`; strict nonempty rectangular JSON `heights` in `[z][x]` order; clip at map edge |
| Raise/depress | `terrain.hill` | Normalized center `.5,.5`, radius `.2`, signed height `.5`, smoothness `1`; cosine-power falloff |
| Generate noise | `terrain.perlin` | Full map; scale `20`, multiplier `.3`, octaves `4`, persistence `.5`, lacunarity `2`, seed `0` records a fresh seed |
| Smooth | `terrain.smooth` | Normalized center `.5,.5`, radius `.1`, iterations `1`; `0` is a no-op; maximum `64`; 3x3 mean preserves outer border |
| Blend to target | `terrain.flatten` | Local circular normalized center `.5,.5`, radius `.1`, `targetHeight=.5`, strength `1`; radius `0` selects one pixel |
| Paint existing layer | `terrain.paint-texture` | Normalized center `.5,.5`, layer `0`, strength `1`, brushSize `10` alphamap pixels; radial brush remains centered when clipped |

Creation accepts only resolutions `33,65,129,257,513,1025,2049,4097` and requires an existing asset parent folder. Explicit asset paths never overwrite. Pixel rectangle values clamp to `0..1` and ragged, null, string, and nonfinite values refuse before mutation. Hill and flatten centers/radii, paint centers/strength, and all sizes must satisfy their documented finite ranges. Perlin accepts `1..32` octaves, nonnegative scale/persistence, positive lacunarity, and a finite signed multiplier; derived samples must remain finite and within the supported range. Large full-map writes and many smoothing passes can take time.

```bash
unity --json command --project-path "$PROJECT_PATH" terrain.create -- --nameTerrain Ground --resolution 513 --dryRun true
unity --json command --project-path "$PROJECT_PATH" terrain.create -- --nameTerrain Ground --resolution 513 --confirm true
unity --json command --project-path "$PROJECT_PATH" terrain.inspect -- --target "$TERRAIN_COMPONENT_ID"
unity --json command --project-path "$PROJECT_PATH" terrain.hill -- --target "$TERRAIN_COMPONENT_ID" --centerX 0.5 --centerZ 0.5 --radius 0.2 --height 0.5 --dryRun true
unity --json command --project-path "$PROJECT_PATH" terrain.hill -- --target "$TERRAIN_COMPONENT_ID" --centerX 0.5 --centerZ 0.5 --radius 0.2 --height 0.5 --confirm true
```
