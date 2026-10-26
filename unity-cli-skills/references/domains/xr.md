# XR scene authoring

Read [foundation routing and safety](foundation.md), [exact component identity](component.md), and [package lifecycle](package.md). XR commands live in a separate optional Editor assembly. The supported package gates are XRI `[3.6.1,4.0.0)` and XR Core Utils `[2.6.0,3.0.0)`; verification uses XRI 3.6.1, Core Utils 2.6.0, Input System 1.20.0 and uGUI 2.6.0. The core command package has no XR assembly reference. Discover the live command catalog after installing the requested dependencies through the package owner; package installation requires its existing authorization and reload workflow.

Every mutation requires Edit mode, a loaded regular scene, and `confirm=true` or `dryRun=true`. Asset, Prefab Stage, preview, stale and wrong-type targets are refused. Exact Component handles are required where the command names a component; composite additions accept an exact GameObject or Component handle for its scene object. Supplied enum values must be named values, never numeric strings. Finite/range, JSON, identity and singleton ambiguity checks run before Undo or writes. Mutations share one Undo step, mark the scene dirty, and return actual state in `Result.State`, issues in `Result.Issues`, plus `Applied`, `DryRun` and `SceneDirty`. Reacquire handles after Undo or scene reopen. Check nested `Ok`/`Error`; save through the existing scene owner.

| Intent | Command and key inputs |
| --- | --- |
| Check setup and obtain scene report | `xr.scene-report`: `verbose=false`, `includeInactive=true` |
| Ensure manager | `xr.manager-ensure`: optional exact `manager`, default name `XR Interaction Manager` |
| Create rig | `xr.rig-create`: name `XR Origin`, `x/y/z=0`, `cameraYOffset=1.36144` |
| Add/configure ray | `xr.interactor-ray`: exact `target`, `maxDistance=30`, `lineType=StraightLine`, `addLineVisual=true` |
| Add/configure direct | `xr.interactor-direct`: exact `target`, `radius=0.1` |
| Add/configure socket | `xr.interactor-socket`: exact `target`, `showHoverMesh=true`, `recycleDelay=1` |
| Add/configure grab | `xr.interactable-grab`: exact `target`, `movementType=VelocityTracking`, throw/smoothing true, smoothing amounts 5, new-body gravity true/kinematic false, optional `attachTransformOffset="x,y,z"` |
| Add/configure simple | `xr.interactable-simple`: exact `target` |
| Configure existing interactable | `xr.interactable-configure`: exact Component `target`, strict JSON `properties` |
| Wire teleport locomotion | `xr.locomotion-teleport`: exact origin object/component `target`, or singleton origin |
| Wire continuous movement | `xr.locomotion-move`: origin `target`, `moveSpeed=2`, `enableStrafe=true`, `enableFly=false` |
| Wire snap/continuous turn | `xr.locomotion-turn`: origin `target`, `turnType=Snap` or `Continuous`, `turnAmount=45`, `turnSpeed=90` |
| Add teleport surface | `xr.teleport-area`: exact `target`, `matchOrientation=WorldSpaceUp`, optional exact `provider` |
| Create anchor | `xr.teleport-anchor-create`: name `Teleport Anchor`, world `x/y/z`, `rotY`, `matchOrientation=TargetUpAndForward`, optional exact `parent` object and `provider` |
| Ensure/convert EventSystem | `xr.event-system-ensure`: optional exact EventSystem Component `target`, otherwise singleton |
| Convert Canvas | `xr.canvas-convert`: exact Canvas Component `target` |
| Configure event haptics | `xr.haptics-configure`: exact interactor Component `target`, select `.5/.1`, hover `.1/.05`, optional exact `output` Component |
| Set XRI layers | `xr.layers-set`: exact interactor/interactable Component `target`, comma-separated `layers=Default` |
| List interactors/interactables | `xr.scene-report --verbose true`: filter its exact typed component entries and counts |
| Wire persistent interaction event | existing `event.listener-add`, described in [UnityEvents](event.md) |

Manager-dependent additions resolve one scene manager or an explicit exact `manager` Component. Ensure the manager first. More than one manager requires an explicit selection; commands do not choose an arbitrary first match. Origin/provider/EventSystem defaults similarly refuse ambiguity.

## Rig, input and runtime prerequisites

The rig creates Origin → Camera Offset → Main Camera, Left Controller and Right Controller. The three pose drivers share the offset space. The Origin's actual `CameraYOffset` is assigned, with Device tracking-origin mode, and the report includes the actual offset Y. Main Camera is tagged and uses near clip `.01`. HMD inline position/rotation/tracking-state actions bind `<XRHMD>/centerEyePosition`, `centerEyeRotation`, `trackingState`; controllers bind `<XRController>{LeftHand}` / `{RightHand}` device pose and tracking-state controls. Input actions follow the standard pose-driver lifecycle.

This configures pose bindings. Select, activate, UI press and application actions still need their supported XRI input reader configuration, and an XR loader/device must exist. For a controller interaction setup, inspect the installed XRI package and its available Starter Assets sample, import that sample through the existing package/sample workflow if authorized, and inspect its configured input-action references and controller prefabs through the asset/component owners. Use the supported prefab and component property commands to instantiate/configure the selected sample rig or to assign actual reader action references on the authored controllers. Inspect real installed public properties and action assets before assignment; do not infer sample paths, action names or device readiness from the factory result. Never install OpenXR or another loader merely because rig authoring succeeded.

New continuous move and turn providers receive inline left/right `primary2DAxis` readers. Updating an existing provider preserves both reader objects and their configured modes, actions, references and manual values. Results report each reader mode, inline and referenced action bindings, and the effective configured bindings. These bindings require runtime controller devices. Locomotion references are provider `mediator` → same-object `XRBodyTransformer.xrOrigin`; `mediator.bodyTransformer` is initialized in normal `Awake` and can be null in Edit mode. Results distinguish configured transformer/origin from the lifecycle-resolved transformer. Teleport destinations disclose a missing provider. Existing unrelated locomotion providers remain; choose the enabled comfort combination deliberately through the component owner.

`xr.scene-report` includes standalone and disabled XR line visuals; `includeInactive` controls inactive scene objects in the inventory. Verbose socket entries include `socketActive`; snap turn entries include `enableTurnLeftRight` and `enableTurnAround`. Multiple managers produce an ambiguity warning.

Pose diagnostics inspect the assigned camera's driver and at least two distinct non-camera controller driver objects. The assigned floor offset must be below the Origin, and the camera and controller drivers must share its subtree. Each driver needs position, rotation and tracking-state action bindings; inline actions and custom action references are accepted. Missing assignments/drivers, misplaced objects and unbound inputs produce separate issues. This checks authoring configuration; it does not establish runtime tracking or hardware readiness.

## Interaction, geometry and UI

Ray interactors add a `.01` white LineRenderer when missing and optionally an XR line visual, with no collider requirement. Direct/socket additions create a trigger SphereCollider only when no Collider exists (radii `.1` / `.15`). Existing colliders remain and results report actual shape/trigger/radius and issues. Socket hover meshes use `showInteractableHoverMeshes`; recycle delay uses `recycleDelayTime` and may be ignored by XRI when hover snapping is enabled.

Grab additions create a Rigidbody only when absent, preserving existing body settings. Missing colliders use the existing mesh with a convex MeshCollider, otherwise a BoxCollider. Simple additions create only a missing BoxCollider. Requested attach offsets create an Attach Point child with local coordinates. Existing trigger colliders are reported rather than silently rewritten.

`xr.interactable-configure` accepts only supplied fields: `selectMode`, `movementType`, `throwOnDetach`, `smoothPosition`, `smoothRotation`, `smoothPositionAmount`, `smoothRotationAmount`, `trackPosition`, `trackRotation`. Grab-only fields on simple interactables, duplicate/unknown JSON fields, wrong types, invalid named enums and nonfinite/negative amounts refuse the entire request before writing.

```bash
unity --json command --project-path "$PROJECT_PATH" xr.interactable-configure -- \
  --target "$GRAB_COMPONENT_ID" --properties '{"smoothPosition":false,"movementType":"Kinematic"}' --dryRun true
```

Area authoring preserves existing colliders and creates a missing mesh/box detection surface. Anchors keep requested world pose under an optional parent, create a destination child, a `(1,.01,1)` detection box and a flat `(1,.02,1)` cylinder indicator with its primitive collider removed and a scene-owned Sprites/Default cyan material `(0,.8,1,.5)`.

EventSystem conversion removes only `StandaloneInputModule` and adds `XRUIInputModule`. Canvas conversion requires an existing Canvas, changes to WorldSpace, removes standard GraphicRaycasters, and adds TrackedDeviceGraphicRaycaster. Size `400×300` and local scale `.001` apply only on mode conversion; existing world-space layout is preserved. Add real visible content, use an exact observer camera and retain an actual screenshot when verifying authoring layout through [lighting/camera capture](lighting-camera.md).

## Haptics, events and layers

Haptics use public `SimpleHapticFeedback` plus `HapticImpulsePlayer`, with `SetInteractorSource` and select/hover amplitude/duration data. Amplitudes must be finite in `[0,1]`; durations must be finite and nonnegative. An explicit output must be a scene Component implementing public `IXRHapticImpulseProvider`. Otherwise an existing parent provider is resolved; a missing provider is disclosed, including socket sources. Feedback configuration and event routing do not prove physical vibration. Tests use public test channels in normal Play lifecycle; hardware support is a separate check.

Use the existing Event owner with exact source/listener Components and the exact public event/method names. Generic XRI events accept only a static no-argument callback with `argType=void`; Unity ignores their payload for this callback. Inspect persistent counts and actual metadata, save, and verify after reopening. Generic `event.invoke` and generic batch addition remain refused.

```bash
unity --json command --project-path "$PROJECT_PATH" event.listener-add -- \
  --target "$INTERACTABLE_COMPONENT_ID" --eventName selectEntered \
  --listenerTarget "$CALLBACK_COMPONENT_ID" --methodName OnSelected --argType void --dryRun true
```

InteractionLayerMask names are XRI interaction layers, separate from Unity Physics layers. `xr.layers-set` validates every supplied name before changing the mask and reports actual mask and names. Unknown or empty names refuse the request.
