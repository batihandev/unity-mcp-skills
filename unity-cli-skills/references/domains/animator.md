# Animator workflows

Read [foundation routing and safety](foundation.md), [the optional package boundary](foundation.md#optional-package-boundary), [GameObject operations](gameobject.md), and [component operations](component.md) first. Keep controller asset authoring and scene Animator runtime operations in their existing owners. Resolve scene targets by the exact Animator component handle returned by component discovery; a GameObject name is not an identity.

## Controller assets

Before diagnosing a pose, identify the exact Animator, active controller state, avatar validity,
and update phase that produced it. For imported rigs, verify exporter axes, stable bone identities,
and the generated clip through real Animator playback. Compare measurements from the same frame
and phase; a compensating root rotation does not establish that the import or retargeting is correct.

The Unity Pipeline package owns controller assets through six native commands. Discover their current schemas with `unity --json command --project-path "$PROJECT_PATH" --query animation/animator --detail full` before using them:

| Intent | Native command | Contract |
|---|---|---|
| Create a controller | `create_animator_controller` | Compose `path` from `folder/name.controller`, using `Assets/Animations` when the folder is omitted. The path is project relative and ends in `.controller`; creation refuses an occupied path. It creates parent folders and a default Base Layer. Leave `confirm=false` in a create-new workflow. |
| Add a parameter | `add_animator_parameter` | `controller`, `name`, `type`, and optional typed `defaultValue`. Float, int, bool, and trigger are supported. Native defaults are float zero, int zero, bool false; duplicate names refuse before writing. |
| Add a layer | `add_animator_layer` | Adds the named layer with weight and blending mode. |
| Add a state | `add_animator_state` | `controller`, exact layer, `name`, optional `motion`, `isDefault`, and graph position. An omitted motion creates a motionless state. Resolve a supplied AnimationClip or BlendTree before dispatch; a missing or wrong motion reference refuses before state creation. |
| Add a transition | `add_animator_transition` | Resolve exact layer and source/destination states before dispatch. Supply `hasExitTime=true` and `duration=0.25` to preserve the established workflow defaults. Native also supports documented AnyState, Entry, Exit, and conditions. |
| Read a controller | `get_animator_controller` | Returns controller identity, typed parameters, layers, states, motion/default flags, and transitions. Use it to verify saved asset changes after writes. |

Asset authoring writes are persistent and have no Undo guarantee. After each write, call `get_animator_controller` again and compare the saved structure. Use the native command names shown above for these intents.

## Assign a controller to a scene Animator

Use `component.member-set` for `runtimeAnimatorController`. Before adding an Animator, resolve and type-check the controller asset as a `RuntimeAnimatorController`; a wrong or missing controller must not leave a new component behind. If the Animator already exists, retain its exact component handle. Then set the `runtimeAnimatorController` member with the exact asset reference, check the typed result's `Ok`, and read the member back. This component mutation uses its existing Undo scope. If adding the Animator, use `add_component`, retain its returned component `instanceId` even when its `globalId` is zero, and pass that exact decimal handle to subsequent commands.

## Runtime parameter changes and state playback

The package's typed runtime commands require a currently playing Editor session, an initialized Animator, and a non-null controller. They do not record Undo because parameter values, triggers, and playback are transient runtime state.

```bash
unity --json command --project-path "$PROJECT_PATH" animator.runtime-set-parameter -- \
  --target "$ANIMATOR_HANDLE" --name Speed --type float --floatValue 2.5

unity --json command --project-path "$PROJECT_PATH" animator.runtime-play -- \
  --target "$ANIMATOR_HANDLE" --stateName 'Base Layer.Idle' --layer 0 --normalizedTime 0
```

`animator.runtime-set-parameter` accepts `float`, `int`, `bool`, or `trigger`. It finds one exact parameter name on the active controller and requires the requested type to match. Float inputs must be finite. Float, int, and bool results include the value read back from the Animator; a mismatch returns an error with the observed value. A trigger result confirms `SetTrigger` was called. Unity exposes no public getter for trigger state, so it does not claim trigger readback.

`animator.runtime-play` requires a finite normalized time and an existing layer index. Supply an exact layer-qualified path such as `Base Layer.Idle` or `Base Layer.Sub.Walk`; a bare name such as `Walk` is accepted only when exactly one state in that layer has that name, including nested state machines. An ambiguous bare name or missing path refuses before playback. The command resolves the canonical full path, checks that the Animator reports its hash on the requested layer, and dispatches that hash. The result retains the requested `State` and returns `ResolvedStatePath`. `Applied=true` confirms the call; `ObservedStateMatched` says whether the immediate current state's full-path hash matches the resolved state. Unity may evaluate the requested state and playback time on a later frame. After a bounded Editor update, inspect `GetCurrentAnimatorStateInfo(layer)` and verify both its full-path hash and normalized time, even if `ObservedStateMatched=true`. The returned `ActualNormalizedTime` and `FullPathHash` describe only the immediate observation, which may still reflect the previous state or playback time. A stale or non-Animator target, stopped Editor, missing controller, or missing layer/state returns a typed error. Both commands resolve only the supplied exact component handle.

## Bounded read-only Animator evaluation

Use `eval` only for the Animator fields and state metadata absent from the native controller summary. Discover the command schema first. Pass the exact selected Animator handle as a decimal string. This compact body reports the selected component's controller, speed, root motion, update/culling mode, layer and parameter counts, plus the owning GameObject identity:

```csharp
var idText = "18446744073709542611";
if (!ulong.TryParse(idText, System.Globalization.NumberStyles.None,
        System.Globalization.CultureInfo.InvariantCulture, out var id))
    throw new System.ArgumentException("Animator handle must be unsigned decimal");
var animator = UnityEditor.EditorUtility.EntityIdToObject(
    UnityEngine.EntityId.FromULong(id)) as UnityEngine.Animator;
if (animator == null) throw new System.ArgumentException("Animator handle is stale or has the wrong type");
var controller = animator.runtimeAnimatorController;
return new {
    target = UnityEngine.EntityId.ToULong(animator.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture),
    gameObject = new {
        handle = UnityEngine.EntityId.ToULong(animator.gameObject.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture),
        name = animator.gameObject.name
    },
    controller = controller == null ? null : UnityEditor.AssetDatabase.GetAssetPath(controller),
    hasController = controller != null,
    speed = animator.speed,
    applyRootMotion = animator.applyRootMotion,
    updateMode = animator.updateMode.ToString(),
    cullingMode = animator.cullingMode.ToString(),
    layerCount = animator.layerCount,
    parameterCount = animator.parameterCount
};
```

For controller state metadata, resolve the exact controller asset and select one layer by index. The state result includes the recipe-level tag and speed alongside the native summary's motion/default information:

```csharp
var path = "Assets/Animations/Actor.controller";
var layerIndex = 0;
var controller = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(path);
if (controller == null) throw new System.ArgumentException("Controller asset is missing or has the wrong type");
if (layerIndex < 0 || layerIndex >= controller.layers.Length)
    throw new System.ArgumentOutOfRangeException(nameof(layerIndex));
var selectedLayer = controller.layers[layerIndex];
var children = selectedLayer.stateMachine.states;
var states = new System.Collections.Generic.List<object>();
for (var i = 0; i < children.Length; i++) {
    var state = children[i].state;
    if (state == null) continue;
    states.Add(new {
        name = state.name,
        tag = state.tag,
        speed = state.speed,
        hasMotion = state.motion != null,
        isDefault = selectedLayer.stateMachine.defaultState == state
    });
}
return new { path, layer = layerIndex, layerName = selectedLayer.name, states };
```

These bodies are read-only. They do not replace the native controller reader or mutate scene state. Keep the exact handle and result with the verification record; do not infer a selected Animator from its GameObject name.
