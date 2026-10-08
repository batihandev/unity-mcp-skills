# Lighting and camera workflows

Read [foundation routing and safety](foundation.md), [GameObjects](gameobject.md), and
[Components](component.md) before editing scene objects. Use native exact-target creation and
component commands for GameObjects, Lights, Cameras, and probes. The optional
`com.batihandev.unity-cli-commands` package supplies the focused Scene View commands and
`component.member-set`, used here for Light and Camera culling-mask writes.

## Lights and probes

Create a named object with `create_gameobject`, add the exact `Light`, `LightProbeGroup`, or
`ReflectionProbe` component with `add_component`, and use `set_transform` for world placement.
Pass only the returned numeric `instanceId` as the new component ObjectRef; creation can return
an empty or zero-valued `globalId` for a transient object. Resolve existing objects by exact
EntityId or project asset path. Do not select by name when more than one object can match.

Use `get_component_properties` for serialized fields and public member evaluation for getter values
the fixed native projection omits. Light serialized fields include `m_Shadows.m_Type` with the
labels `No Shadows`, `Hard Shadows`, and `Soft Shadows`; the public `Light.enabled` getter supplies
the enabled state. Set `Light.cullingMask` through the existing `component.member-set` command after
validating each layer name and computing the full integer mask. Unknown names refuse the item before
any write.

`LightProbeGroup.m_SourcePositions` is an ordered array of positions. `ReflectionProbe` fields
include `m_BoxSize` and `m_Resolution`. Read them back after writes. Record Undo only after the
mutation command reports a successful write, then verify the restored values. Leave omitted fields
unchanged; range and spot-angle values do not apply to every light type.

For `light_add_probe_group`, resolve one exact GameObject and inspect it for an existing
`LightProbeGroup` before adding anything. Reuse that component if present (`existed=true`);
otherwise add it through native `add_component` (`existed=false`). The default grid dimensions
are `(0,0,0)` and spacings are `(2,1.5,2)`. Generate positions only when **all** three
dimensions are positive. Iterate Y, then X, then Z, with Z changing fastest; position
`(ix,iy,iz)` is `(ix*spacingX-(gridX-1)*spacingX/2, iy*spacingY,
iz*spacingZ-(gridZ-1)*spacingZ/2)`. Thus Y starts at zero while X and Z are centered.
Write the complete ordered `m_SourcePositions` through `set_component_properties` only for a
generated grid, and return the exact object identity, `probeCount` from the final positions,
`existed`, and `hasGrid`. With any nonpositive dimension, preserve an existing group's positions
and report `hasGrid=false`; a new group's native initial positions remain its readback.

For `light_find_all`, traverse only the active scene, including inactive objects. Read exact
`Light` components and project each to `{name, instanceId, path, lightType, intensity, enabled}`;
`instanceId` is the GameObject's decimal EntityId, and `path` is its full hierarchy path so
duplicate names remain distinguishable. Parse an optional `LightType` case insensitively and
refuse an unknown type; do not silently expand an invalid filter. Apply a nonnegative `limit`
after filtering in deterministic hierarchy preorder; the default limit is 50. `count` is the number **returned** after
limiting, matching the original command, rather than the unbounded match total. A zero limit
returns an empty array and count zero. Use native `find_gameobjects` for bounded discovery and
public getters for fields absent from its projection; verify the selected component identity.

For a batch, preflight every supplied enum and layer name before dispatching an item. Valid items
continue independently and report their exact target and result. Invalid light types, shadow labels,
or search filters refuse that item without broadening a search or leaving a partially changed
component. Existing native commands own the writes; no `light.*` compatibility command is required.

Use `get_lighting_settings` and `lighting_bake_status` for project illumination metadata. A project
without a `LightingSettings` asset may report the native settings as unavailable. If a workflow
needs a complete settings readback, make the asset an explicit owned fixture input and restore its
captured values after the case.

The complete `light_get_lightmap_settings` result is `{success, bakedGI, realtimeGI,
lightmapSize, lightmapPadding, isRunning, lightmapCount}`. Read `Lightmapping.bakedGI` and
`Lightmapping.realtimeGI` with a present `LightingSettings` asset, `LightmapEditorSettings.maxAtlasSize`
and `.padding`, `Lightmapping.isRunning`, and `LightmapSettings.lightmaps.Length` through a bounded
read-only Editor evaluation. `lightmapCount` counts lightmaps, not Light Probes. Native
`get_lighting_settings` and `lighting_bake_status` provide complementary details; neither alone
proves all seven fields. Treat a missing settings asset as unavailable rather than inventing GI
values. Do not start a bake for a read-only request.

## Game Cameras

Create cameras through `create_gameobject` plus `add_component` and `set_transform`. Use
`get_component_properties` for serialized fields and bounded public member reads for properties the
native projection omits. The serialized field names verified for Camera are `near clip plane`,
`far clip plane`, `field of view`, `orthographic`, `orthographic size`, `m_BackGroundColor`, and
`m_Depth`. `m_ClearFlags` uses `Skybox`, `Solid Color`, `Depth only`, or `Don't Clear`.

Set `Camera.cullingMask` through `component.member-set` only after all requested layer names resolve;
unknown names refuse before the full mask is changed. Orthographic size is conditional on
orthographic projection. Preserve unspecified fields. Verify readback and one Undo step for each
successful native component write.

For `camera_get_properties`, resolve one exact Camera and return `{success, name, fieldOfView,
nearClipPlane, farClipPlane, orthographic, orthographicSize, depth, cullingMask, clearFlags,
backgroundColor:{r,g,b,a}, rect:{x,y,w,h}}`. Read the public `Camera` getters in one bounded
read-only evaluation tied to its exact EntityId. The native serialized property map can corroborate
optical fields, but its `LayerMask` and `Rect` placeholders are not the required values. Report
`clearFlags` as the public enum name and preserve the actual float channels and viewport values.

For `camera_list`, traverse the active scene including inactive GameObjects and project every
Camera to `{name, instanceId, path, depth, orthographic, enabled}`. `instanceId` is its
GameObject EntityId, and `path` disambiguates duplicate names. Sort by ascending `Camera.depth`,
retaining hierarchy traversal order for ties, and return `count` equal to the complete array
length. Do not select a camera by name to populate this list.

The exact-camera screenshot workflow and its host PNG publication transaction are in
[Scene operations](scene.md#screenshot-workflow). Its camera-specific default `savePath` is
`Assets/screenshot.png`; default dimensions are 1920×1080, and an explicit caller `savePath`
replaces that default. Normalize an
extensionless project-relative path to `.png`, require confinement under `Assets/`, and refuse
other extensions or a preexisting destination without the transaction's explicit replacement
authorization. It uses the normalized requested project-relative path,
validates bounded positive dimensions, restores camera and render-target state, checks the PNG IHDR,
and publishes only through the existing host authoring transaction. Asset import and readback are a
separate step.

## Scene View

Scene View state is editor state, separate from Game Camera components and scene Undo. With the
optional package installed, `scene.view-info` reads the current active view, `scene.view-frame` sets
a world-space pivot with optional Euler rotation and size, and `scene.view-align` aligns to one exact
GameObject transform. These commands refuse when the requested exact target or existing active view
is unavailable; they never create a view.

Omitted rotation and size preserve the current values. `instant=true` uses the direct LookAt path;
`instant=false` requests the animated LookAt path. Results distinguish the requested pivot and view
state from the sampled Scene View camera pose. Animated framing and transform alignment may settle
over later Editor updates; poll `scene.view-info` across updates before asserting the final camera
pose. Alignment targets the GameObject transform, not its bounds.

For application-owned observer framing, see the reusable
[ObserverFraming example](../examples/ObserverFraming.cs). It checks measured room and subject
bounds, the complete subject frustum, explicit candidate order, and obstacle-mask line of sight
before applying a candidate. Keep room measurements, subject selection, and candidate positions in
the consuming application.
