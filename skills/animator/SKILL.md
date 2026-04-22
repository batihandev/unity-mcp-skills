---
name: unity-animator
description: "Use when users want to create animation controllers, add parameters, play animations, or control animation state machines."
---

# Unity Animator Skills

## Overview

Control Unity's animation system - create controllers, manage parameters, and control playback.

## Common Mistakes


**DO NOT** (common hallucinations):
- `animator_create_clip` / `animator_add_clip` do not exist → AnimationClips are created via Unity Editor or asset import
- `animator_set_speed` does not exist → use `component_set_property` on Animator component with propertyName="speed"

**Routing**:
- For Timeline animation → use `timeline` module
- For component properties on Animator → use `component` module
- For animation import settings → use `importer` module

## Skills Overview

| Skill | Description |
|-------|-------------|
| `animator_create_controller` | Create new Animator Controller |
| `animator_add_parameter` | Add parameter to controller |
| `animator_get_parameters` | List all parameters |
| `animator_set_parameter` | Set parameter value at runtime |
| `animator_play` | Play animation state |
| `animator_get_info` | Get Animator component info |
| `animator_assign_controller` | Assign controller to GameObject |
| `animator_list_states` | List states in controller |
| `animator_add_state` | Add a state to a controller layer |
| `animator_add_transition` | Add a transition between two states |

---

## Parameter Types

| Type | Description | Example Use |
|------|-------------|-------------|
| `float` | Decimal value | Speed, blend weights |
| `int` | Integer value | State index |
| `bool` | True/false | IsGrounded, IsRunning |
| `trigger` | One-shot signal | Jump, Attack |

---

## Skills

### animator_create_controller
Create a new Animator Controller.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `name` | string | Yes | - | Controller name |
| `folder` | string | No | "Assets/Animations" | Save folder |

**Returns**: `{success, name, path}`

### animator_add_parameter
Add a parameter to a controller.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `controllerPath` | string | Yes | - | Controller asset path |
| `paramName` | string | Yes | - | Parameter name |
| `paramType` | string | Yes | - | float/int/bool/trigger |
| `defaultFloat` | float | No | 0 | Initial float value |
| `defaultInt` | int | No | 0 | Initial int value |
| `defaultBool` | bool | No | false | Initial bool value |

### animator_get_parameters
Get all parameters from a controller.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `controllerPath` | string | Yes | Controller asset path |

**Returns**: `{success, parameters: [{name, type, defaultFloat/defaultBool/...}]}`

### animator_set_parameter
Set a parameter value at runtime.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | Yes | GameObject name |
| `paramName` | string | Yes | Parameter name |
| `paramType` | string | Yes | float/int/bool/trigger |
| `floatValue` | float | No* | Float value |
| `intValue` | int | No* | Integer value |
| `boolValue` | bool | No* | Boolean value |

*Use the appropriate value for paramType (trigger doesn't need a value)

### animator_play
Play a specific animation state.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `name` | string | Yes | - | GameObject name |
| `stateName` | string | Yes | - | Animation state name |
| `layer` | int | No | 0 | Animator layer |
| `normalizedTime` | float | No | 0 | Start time (0-1) |

### animator_get_info
Get Animator component information.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | Yes | GameObject name |

**Returns**: `{gameObject, instanceId, hasController, controllerPath, speed, applyRootMotion, updateMode, cullingMode, layerCount, parameterCount}`

### animator_assign_controller
Assign a controller to a GameObject.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | Yes | GameObject name |
| `controllerPath` | string | Yes | Controller asset path |

### animator_list_states
List all states in a controller layer.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `controllerPath` | string | Yes | - | Controller asset path |
| `layer` | int | No | 0 | Layer index |

**Returns**: `{success, states: [{name, tag, speed}]}`

### animator_add_state
Add a state to an Animator Controller layer.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `controllerPath` | string | Yes | - | Controller asset path |
| `stateName` | string | Yes | - | Name for the new state |
| `clipPath` | string | No | null | Animation clip asset path to assign |
| `layer` | int | No | 0 | Layer index |

**Returns**: `{success, controller, stateName, layer}`

### animator_add_transition
Add a transition between two states in an Animator Controller.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `controllerPath` | string | Yes | - | Controller asset path |
| `fromState` | string | Yes | - | Source state name |
| `toState` | string | Yes | - | Destination state name |
| `layer` | int | No | 0 | Layer index |
| `hasExitTime` | bool | No | true | Whether transition waits for exit time |
| `duration` | float | No | 0.25 | Transition duration in seconds |

**Returns**: `{success, from, to, layer, hasExitTime, duration}`

---

## Recipe Path

Recipe path rule: `../../recipes/animator/<command>.md`

See [../../recipes/animator/README.md](../../recipes/animator/README.md) for the full command index.

## Best Practices

1. Create controller before adding parameters
2. Use meaningful parameter names
3. Triggers reset automatically after firing
4. Set parameters before playing states
5. Use layers for independent animations (body + face)
6. States must exist in controller before playing

---
