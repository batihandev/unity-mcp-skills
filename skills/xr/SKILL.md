---
name: unity-xr
description: "Use when users want to set up XR rigs, add grab interactions, configure teleportation, continuous locomotion, XR UI, or diagnose XR scenes. Requires com.unity.xr.interaction.toolkit 3.x on Unity 6+."
---

# Unity XR Interaction Toolkit Skills

## Requirements

- `com.unity.xr.interaction.toolkit` (≥ 3.4). Recipes target XRI 3 API only. If missing, install via `Unity_PackageManager_ExecuteAction` with `operation=Add`, `package=com.unity.xr.interaction.toolkit`, `version=3.4.1`.

## When to Use

Use this module for XR Interaction Toolkit setup and configuration.

> **Hard rule**: Read this file before the first `xr_*` call in a session. Wrong property names can fail silently because the bridge is reflection-based.

## Common Mistakes

**DO NOT** (common hallucinations):
- `XRHand`, `XRPlayer`, `XRTeleporter`, `GrabInteractor`, `VRController`, `XRLocomotion`, and `XRManager` are not the runtime classes you want here
- `interactable.OnGrab()` / `OnRelease()` are not the XRI event model -> use `selectEntered` / `selectExited`
- `controller.vibrate()` is not the documented route here -> configure haptics through `xr_configure_haptics`
- Do not assume physics Layer or Tag replaces `InteractionLayerMask`
- Do not guess component property names — they are set via reflection. Use the skill parameters directly

**Correct class names to anchor on**:
- `XRInteractionManager`
- `XROrigin`
- `XRRayInteractor`
- `XRDirectInteractor`
- `XRSocketInteractor`
- `XRGrabInteractable`
- `XRSimpleInteractable`
- `TeleportationProvider`
- `TeleportationArea`
- `TeleportationAnchor`
- `ContinuousMoveProvider`
- `SnapTurnProvider`
- `ContinuousTurnProvider`
- `TrackedDeviceGraphicRaycaster`
- `XRUIInputModule`
- `TrackedPoseDriver`

**Routing**:
- For non-XR Canvas UI creation -> use `ui`

## Quick Reference

### Setup and Diagnostics

| Skill | Use | Key parameters |
|-------|-----|----------------|
| `xr_check_setup` | Validate XR package, rig, managers, and scene prerequisites | `verbose?` |
| `xr_setup_rig` | Create XR Origin + camera + controllers | `name`, `cameraYOffset?` |
| `xr_setup_interaction_manager` | Add or find manager | none |
| `xr_setup_event_system` | Replace/add XR UI input stack | none |
| `xr_get_scene_report` | Report scene-side XR status | none |

### Interactors and Interactables

| Skill | Use | Key parameters |
|-------|-----|----------------|
| `xr_add_ray_interactor` | Remote pointing / ray interaction | `name`, `maxDistance?`, `lineType?` |
| `xr_add_direct_interactor` | Close-range hand grab | `name`, `radius?` |
| `xr_add_socket_interactor` | Snap-to slot | `name`, `showHoverMesh?`, `recycleDelay?` |
| `xr_add_grab_interactable` | Rigidbody + collider + grab config | `name`, `movementType?`, `throwOnDetach?` |
| `xr_add_simple_interactable` | Hover/select without grab | `name` |
| `xr_configure_interactable` | Fine-tune interactable behavior | target + changed fields only |
| `xr_list_interactors` | List all scene interactors | none |
| `xr_list_interactables` | List all scene interactables | none |

### Locomotion and XR UI

| Skill | Use | Key parameters |
|-------|-----|----------------|
| `xr_setup_teleportation` | Add teleport provider to XR Origin | none |
| `xr_add_teleport_area` | Mark a surface as teleportable | `name`, `matchOrientation?` |
| `xr_add_teleport_anchor` | Create a stationary teleport destination | `name`, `x/y/z`, `rotY?`, `matchOrientation?` |
| `xr_setup_continuous_move` | Add stick locomotion | `moveSpeed?`, `enableStrafe?`, `enableFly?` |
| `xr_setup_turn_provider` | Add snap or smooth turn | `turnType`, `turnAmount?`, `turnSpeed?` |
| `xr_setup_ui_canvas` | Convert Canvas for XR interaction | `name` |

### Feedback and Filtering

| Skill | Use | Key parameters |
|-------|-----|----------------|
| `xr_configure_haptics` | Set hover/select vibration | `name`, intensities, durations |
| `xr_add_interaction_event` | Wire interaction callback to target method | `name`, `eventType`, `targetName`, `targetMethod` |
| `xr_configure_interaction_layers` | Set InteractionLayerMask | `name`, `layers`, `isInteractor` |

## Workflow Summary

### Rig Setup

1. Run `xr_check_setup`.
2. Create the rig with `xr_setup_rig`.
3. Ensure XR UI input exists with `xr_setup_event_system`.
4. Add at least one interactor per controller.

### Grab Setup

- Direct hand grab: `xr_add_direct_interactor` + `xr_add_grab_interactable`
- Distance grab: `xr_add_ray_interactor` + `xr_add_grab_interactable`
- Socket placement: `xr_add_socket_interactor` + grabbable object

`movementType` defaults matter:
- `VelocityTracking`: best general-purpose physical grab
- `Kinematic`: use for handles/tools that should not get stuck
- `Instantaneous`: best for precise remote grab

### Locomotion Setup

- Teleport: `xr_setup_teleportation` + `xr_add_ray_interactor` + `xr_add_teleport_area`/`xr_add_teleport_anchor`
- Continuous locomotion: `xr_setup_continuous_move`
- Turn: `xr_setup_turn_provider`

Comfort default: snap turn + moderate move speed (`~2.0 m/s`).

### XR UI

1. Convert Canvas with `xr_setup_ui_canvas`
2. Ensure `xr_setup_event_system`
3. Add a ray interactor on the controller that should click UI

### Events and Haptics

Event types for `xr_add_interaction_event`: `selectEntered`, `selectExited`, `hoverEntered`, `hoverExited`, `firstSelectEntered`, `lastSelectExited`, `activated`, `deactivated`.

Haptic defaults: `selectIntensity=0.5`, `selectDuration=0.1`, `hoverIntensity=0.1`, `hoverDuration=0.05`.

## Collider Configuration Matrix

This is the most important XR anti-hallucination table.

| Component | Collider required | `isTrigger` | Recommended collider | Reason |
|----------|-------------------|-------------|----------------------|--------|
| `XRDirectInteractor` | Yes | **True** | SphereCollider (`0.1-0.25`) | Overlap detection |
| `XRRayInteractor` | No | - | None | Uses raycasts |
| `XRSocketInteractor` | Yes | **True** | SphereCollider (`0.1-0.3`) | Snap zone |
| `XRGrabInteractable` | Yes | **False** | BoxCollider or convex MeshCollider | Physics + ray target |
| `XRSimpleInteractable` | Yes | **False** | BoxCollider | Selection detection |
| `TeleportationArea` | Yes | **False** | MeshCollider or BoxCollider | Surface raycast target |
| `TeleportationAnchor` | Yes | **False** | Thin BoxCollider | Point raycast target |

Critical rules:
1. `XRGrabInteractable` needs a `Rigidbody`.
2. A grabbable collider must **not** be trigger.
3. A direct interactor collider **must** be trigger.
4. Dynamic mesh colliders must be convex.
5. Socket colliders should be trigger-only overlap zones.

## Version Compatibility

| Topic | XRI 2.x | XRI 3.x |
|------|---------|---------|
| Main namespace style | root namespace | split sub-namespaces |
| Rig type | `XROrigin` | `XROrigin` |
| Locomotion core | `LocomotionSystem` | `LocomotionMediator` |
| Controller type | `ActionBasedController` | `ActionBasedController` |
| Bridge behavior | Reflection helper falls back automatically | Reflection helper prefers 3.x first |

## C# Templates

Recipe path rule: `../../recipes/xr/<command>.md`

*See `../../recipes/xr/<command>.md` for C# templates.*

## Important Notes

1. Scenes should normally have exactly one `XRInteractionManager`.
2. Most locomotion skills assume an `XROrigin` already exists.
3. Package installation or reconfiguration can trigger Domain Reload. Wait before retrying XR calls.
4. Use `InteractionLayerMask` for interactor/interactable filtering.
5. For custom XR scripts, prefer XRI lifecycle hooks and events over `Update()` polling when possible.
