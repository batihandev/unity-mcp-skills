# Cinemachine

Use this guide with `com.unity.cinemachine` 6.6.x, its Splines dependency, the selected project's live command catalog, and [foundation safety](foundation.md). The optional command assembly exposes `cinemachine.stage-set`, `cinemachine.group-member`, and `cinemachine.impulse-generate`. Check outer CLI status, native batch and item status, and each typed command's nested `Ok` separately. Retain exact object handles and read the actual Unity state after writes. These commands do not select the first object with a matching name.

## Create and inspect cameras

Select a loaded regular scene, create a uniquely named GameObject with native `create_gameobject`, then add `Unity.Cinemachine.CinemachineCamera` with native `add_component`. For a default virtual camera, set `Priority.Enabled=true` and `Priority.m_Value=10` on its exact component through native `set_serialized_field`; inspect the actual priority. Select the intended Unity Camera explicitly and add `Unity.Cinemachine.CinemachineBrain` there if absent. Do not infer a Brain from `MainCamera` or create one on an unselected Camera. Save the scene after authoring.

For a FreeLook-style camera, add `Unity.Cinemachine.CinemachineOrbitalFollow` (Body) and `Unity.Cinemachine.CinemachineRotationComposer` (Aim), set `OrbitStyle=ThreeRing`, then configure and inspect the three `Orbits` rings. `cinemachine.stage-set` selects an exact `CinemachineCamera`, `Body`/`Aim`/`Noise`, and a full concrete component type or `None`. Preview with `dryRun=true` or apply with `confirm=true`. It validates the stage before removal, refuses duplicate components in one stage, uses the dependency-aware `component.remove`, and records one Undo group. The same type is an idempotent result. Inspect `PreviousType`, actual `ComponentType`, and `SceneDirty`; save the scene. For ordinary additional components and extensions, use native `add_component` with the exact GameObject and full type. Enumerate exact components first and skip adding an extension when that exact type already exists; a direct duplicate native add can fail. Remove only a selected `CinemachineExtension` subtype through `component.remove`.

Use exact component and GameObject inspection from [Components](component.md) to list virtual cameras, stages, extensions, scene targets and property values. For the Brain, inspect its exact `OutputCamera`, `ActiveVirtualCamera`, `ActiveBlend`, `IsBlending` and update method through a bounded read-only `eval` when native inspection omits them. An active camera is a runtime state, so a scene inventory alone does not establish it.

To discover available component types, run this read-only body through discovered `eval`. Use the qualified names when selecting a type to add; inspect attached component handles separately.

```csharp
System.Type[] available;
try { available = typeof(Unity.Cinemachine.CinemachineCamera).Assembly.GetTypes(); }
catch (System.Reflection.ReflectionTypeLoadException error) { available = error.Types; }
var types = available
    .Where(t => t != null && t.IsPublic && !t.IsAbstract && !t.ContainsGenericParameters &&
        t.IsSubclassOf(typeof(UnityEngine.MonoBehaviour)) &&
        t.Name.StartsWith("Cinemachine", System.StringComparison.Ordinal))
    .OrderBy(t => t.Name, System.StringComparer.Ordinal).ToArray();
return new { count = types.Length, components = types.Select(t => t.Name).ToArray(),
    qualifiedTypes = types.Select(t => t.FullName).ToArray() };
```

## Set targets, priority, lens and noise

On `CinemachineCamera`, serialized `Target.TrackingTarget` is the tracking reference; `Target.LookAtTarget` and `Target.CustomLookAtTarget` determine custom aiming. Resolve each requested target to an exact Transform in the same loaded regular scene before editing. An unresolved nonempty name is an error, not a request to clear a target. Use the actual discovered `SerializedProperty` leaf and native `set_serialized_field`. `Follow` and `LookAt` are public derived properties for readback. A clear requires an explicit null reference.

Priority needs both `Priority.Enabled=true` and `Priority.m_Value=<int>`. To activate a selected camera by priority, enumerate loaded Cinemachine cameras in the relevant context, read their effective priorities, check that the maximum can be incremented without overflow, then set the selected camera's enabled priority to maximum plus one. Verify the Brain selected that camera after evaluation; priority alone does not prove the Brain used it.

Lens paths include `Lens.FieldOfView`, `Lens.NearClipPlane`, `Lens.FarClipPlane`, `Lens.OrthographicSize`, and `Lens.ModeOverride`. Validate field of view, clips, and mode before writing; `Orthographic` and `IsPhysicalCamera` are derived read-only properties. For noise, add exact `CinemachineBasicMultiChannelPerlin` as the Noise stage if needed, then set `AmplitudeGain`, `FrequencyGain`, and `NoiseProfile` on that component. Native multi-field `batch(transactional=true,on_error=abort)` supports these scene serialized leaf writes as one Undo group. Preflight paths and values, inspect outer and every item result, then read back the component. A typed result with `Ok=false` is not a native batch failure signal.

## Body, aim and extensions

Discover actual paths on the selected component before writing; do not turn a recipe alias into a guessed member path. The following paths are observed on Cinemachine 6.6 and give the field map for the advertised controls:

| Component | Useful serialized paths |
|---|---|
| `CinemachineFollow` | `FollowOffset.x/y/z`; `TrackerSettings.BindingMode`, `TrackerSettings.PositionDamping.x/y/z`, `TrackerSettings.RotationDamping.x/y/z`, `TrackerSettings.AngularDampingMode`, `TrackerSettings.QuaternionDamping` |
| `CinemachineOrbitalFollow` | `TargetOffset.x/y/z`, `OrbitStyle`, `Radius`, `Orbits.Top/Center/Bottom.Radius/Height`, `Orbits.SplineCurvature`, `TrackerSettings.*` |
| `CinemachineThirdPersonFollow` | `Damping.x/y/z`, `ShoulderOffset.x/y/z`, `VerticalArmLength`, `CameraSide`, `CameraDistance`, `AvoidObstacles.*` |
| `CinemachinePositionComposer` | `CameraDistance`, `DeadZoneDepth`, `Composition.ScreenPosition.x/y`, `Composition.DeadZone.Enabled/Size.x/Size.y`, `Composition.HardLimits.Enabled/Size.x/Size.y/Offset.x/Offset.y`, `Damping.x/y/z`, `TargetOffset.x/y/z`, `Lookahead.*` |
| `CinemachineRotationComposer` | `Composition.ScreenPosition.x/y`, `Composition.DeadZone.Enabled/Size.x/Size.y`, `Composition.HardLimits.Enabled/Size.x/Size.y/Offset.x/Offset.y`, `CenterOnActivate`, `Damping.x/y`, `TargetOffset.x/y/z`, `Lookahead.*` |
| `CinemachinePanTilt` | `ReferenceFrame`, `PanAxis.Value/Center/Range.x/Range.y/Wrap/Recentering.*`, `TiltAxis.Value/Center/Range.x/Range.y/Wrap/Recentering.*` |

`CinemachineFollowZoom` has `Width`, `Damping`, and `FovRange.x/y`; `CinemachineGroupFraming` has `FramingSize`, `Damping`, `FramingMode`, `FovRange.x/y`, and dolly and orthographic ranges. `CinemachineConfiner2D` has `BoundingShape2D`, `Damping`, `SlowingDistance`, and `OversizeWindow.*`. `CinemachineConfiner3D` has `BoundingVolume` and `SlowingDistance`; it has no generic `Damping` path. `CinemachineDeoccluder` owns collision fields under `AvoidObstacles.*`, including `CameraRadius`, `Strategy`, `MaximumEffort`, `SmoothingTime`, and `Damping`; use it for occlusion rather than deprecated `CinemachineCollider`. `CinemachineDecollider` has `CameraRadius`, `Decollision.*`, and `TerrainResolution.*`. Validate object reference types, enum names and finite numeric ranges first. Inspect actual paths on the selected installed version, apply native leaf writes, and read back.

For a generic component property request, follow the [component field ownership rules](component.md): use `set_serialized_field` for an observed serialized owner, and `component.member-set` for a writable public member with no serialized owner. Dot-path reflection guesses and silent enum fallbacks are invalid.

## Managers, groups and spline

Create a uniquely named manager GameObject and add `CinemachineClearShot`, `CinemachineStateDrivenCamera`, `CinemachineSequencerCamera`, or `CinemachineMixingCamera`. Create each virtual-camera child under the exact manager; verify it appears in public `ChildCameras` before mapping or weighting it. ClearShot uses `ActivateAfter`, `MinDuration`, and `RandomizeChoice`. Both ClearShot and StateDriven expose `DefaultBlend.Style/Time`. StateDriven uses exact `AnimatedTarget`, `LayerIndex`, and `Instructions`; validate the chosen Animator, controller and selected layer. Sequencer uses `Loop` (default false) and `Instructions`. MixingCamera weights are `Weight0` through `Weight7`: resolve the exact direct child and its index in `ChildCameras`, refuse index 8 or later, then write the corresponding weight and read `GetWeight(child)`.

Append a StateDriven instruction with a native transactional batch: resize `Instructions.Array.size` by one and set **every** new entry leaf, including `FullHash=Animator.StringToHash(exact full state path)`, `Camera`, `ActivateAfter=0`, and `MinDuration=0` unless supplied. Preserve existing entries and inspect each. Append a Sequencer instruction the same way: resize its `Instructions` list and set every new `Camera`, `Hold=2`, `Blend.Style=EaseInOut`, `Blend.Time=2`, and remaining discovered blend leaves explicitly. `VirtualCamera` is only a historical serialized alias for the current `Camera` field. A resized serialized array may copy the previous last element, so every new field must be initialized before accepting the batch. Inspect batch outer status and every item; read back the complete instruction and save the scene. Validate the state name against the selected layer/controller and child membership before resize.

For a Target Group, create a GameObject and add `CinemachineTargetGroup`. `cinemachine.group-member` accepts exact group and Transform (or GameObject), `action=upsert|remove`, defaults weight/radius to 1, and uses `dryRun` or `confirm`. It refuses a target appearing twice in the existing list; upsert removes an existing single entry and appends a new one, preserving the original ordering rule. Missing removal is idempotent. The result lists actual ordered target handles. Group and target must share a loaded regular scene; save and reopen to verify persistent members.

For a spline dolly, add `CinemachineSplineDolly` to the selected camera and select an exact `UnityEngine.Splines.SplineContainer` in the same scene. Its current `Spline` is a writable public property, so use `component.member-set` with an exact reference and inspect the property. The historical serialized alias `Spline` is not the current field owner. Configure `SplineOffset`, `CameraRotation`, `Damping.*`, and `AutomaticDolly.*` through observed serialized paths.

## Brain and blends

On an exact `CinemachineBrain`, serialized settings include `UpdateMethod`, `BlendUpdateMethod`, `DefaultBlend.Style/Time`, `CustomBlends`, `ShowDebugText`, `ShowCameraFrustum`, and `IgnoreTimeScale`. Validate enum values by name and finite nonnegative blend time, write observed leaf paths, and read back. Per-pair blends belong to a `CinemachineBlenderSettings` asset, not the default blend: create/select an exact asset, export its current Editor JSON, preserve unmatched `CustomBlends` pairs, edit the exact `From`, `To`, `Blend.Style`, and `Blend.Time` fields, then use `scriptableobject.json-import` preview and confirmation. Assign that exact asset to the Brain's `CustomBlends` serialized reference. Reimport/reload and call public `GetBlendForVirtualCameras` for the named pair. Pair names control matching; a successful asset import alone is insufficient proof of selection. Asset and scene writes are separate and need separate save/readback.

## Impulses

Add `CinemachineImpulseSource` to an exact loaded scene object and `CinemachineImpulseListener` to the selected receiving camera or appropriate Brain path. Inspect channel masks and `Gain`, and verify the receiving camera has an active Brain. Modern impulse modes use `ImpulseDefinition.ImpulseDuration` and `DissipationDistance/Rate`; legacy RawSignal mode uses `RawSignal`, `TimeEnvelope.*`, `AmplitudeGain`, `FrequencyGain`, and `ImpactRadius`. Do not apply a legacy amplitude or envelope field as a modern duration.

`cinemachine.impulse-generate` accepts the exact source, finite `velocityX/Y/Z` (default down), and `confirm` or `dryRun`. Preview validates only; a successful confirmed result means `CreateAndReturnEvent` returned an event. It is transient and has no Undo or scene persistence. Prove listener response through an actual output signal or camera movement with the explicit receiving camera; event creation by itself does not prove a visible effect.
