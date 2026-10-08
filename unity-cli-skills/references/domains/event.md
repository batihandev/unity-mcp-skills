# UnityEvents

Read [foundation routing and safety](foundation.md) and [component identity](component.md) first. The optional package owns persistent UnityEvent authoring and explicit invocation through eight `event.*` commands. Use an exact Component handle for the source event and an exact Component or GameObject handle for each listener. Names and hierarchy paths are discovery aids, not command targets. Check the nested `Ok`, `Result`, and `Error` fields after every call.

| Intent | Command |
| --- | --- |
| List events, count listeners, inspect persistent listeners | `event.inspect` |
| Add one listener | `event.listener-add` |
| Add ordered parameterless listeners | `event.listeners-add-batch` |
| Remove one listener | `event.listener-remove` |
| Clear all persistent listeners | `event.listeners-clear` |
| Change listener call state | `event.listener-state` |
| Append eligible listeners from another event | `event.listeners-copy` |
| Invoke once | `event.invoke` |

## Inspect and choose an event

`event.inspect --target "$COMPONENT_ID"` lists each public instance UnityEvent field and readable property, including inherited members, with its type and persistent listener count. A null event member remains in the list with `IsNull=true`, count zero, and an empty listener list when requested; selecting that member for inspection, editing, or invocation refuses it. Add `--eventName onClick` to select one exact non-null member; add `--includeListeners true` for each persistent index, exact target handle, target name/type, method name, and call state. Generic `UnityEvent<T>` values can be inspected. Runtime listeners are not part of Unity's persistent listener list or count.

Persistent edits require a unique serialized backing field for the selected event. A public property such as `Button.onClick` may be edited when it returns such a field. A computed property or an event held only in nonserialized state is refused. The source must be a Component in a regular loaded scene; asset, prefab-stage, preview, stale, and wrong-type targets are refused.

Persistent listener edits, including previews, require Edit mode; `event.inspect` and `event.invoke` remain usable in Play mode.

```bash
unity --json command --project-path "$PROJECT_PATH" event.inspect -- --target "$COMPONENT_ID" --includeListeners true
unity --json command --project-path "$PROJECT_PATH" event.inspect -- --target "$COMPONENT_ID" --eventName onClick --includeListeners true
```

## Add listeners

`event.listener-add` accepts `eventName`, exact `listenerTarget`, and exact `methodName`. The callback must be a public instance method that returns `void` and takes exactly zero arguments (`argType=void`) or one `int`, `float`, `string`, or `bool` argument. A public property setter can be named `set_PropertyName` with its exact parameter type. A parameterless `UnityEvent` source supports all these argument modes. A generic `UnityEvent<T>` source supports only `argType=void`: Unity stores a static no-argument callback through public `UnityEventTools.AddVoidPersistentListener`, ignoring the event payload. Dynamic typed listeners and primitive constants on generic events are refused. Generic batch addition and direct invocation remain outside these commands. The target can be a GameObject for a GameObject method, or a Component for its method.

`argType` defaults to `void`. The matching argument value comes from `intArg`, `floatArg`, `stringArg`, or `boolArg`; a float must be finite. Unity normalizes a null persistent string argument according to its own serialization behavior. `mode` defaults to `RuntimeOnly` and accepts only `Off`, `RuntimeOnly`, or `EditorAndRuntime`, ignoring letter case. Preview with `dryRun=true` and inspect the plan; the actual add is Undoable and reports the observed persistent listener metadata after writing. Save the scene to persist it.

```bash
unity --json command --project-path "$PROJECT_PATH" event.listener-add -- \
  --target "$COMPONENT_ID" --eventName onClick --listenerTarget "$LISTENER_ID" --methodName OnClick --dryRun true
unity --json command --project-path "$PROJECT_PATH" event.listener-add -- \
  --target "$COMPONENT_ID" --eventName onClick --listenerTarget "$LISTENER_ID" --methodName OnClick --mode EditorAndRuntime
unity --json command --project-path "$PROJECT_PATH" event.listener-add -- \
  --target "$COMPONENT_ID" --eventName onClick --listenerTarget "$LISTENER_ID" --methodName OnCount --argType int --intArg 3
```

For a sequence of parameterless `RuntimeOnly` listeners, `event.listeners-add-batch` takes a strict JSON array of objects with exactly `target` and `methodName`. Envelope errors refuse before changes. Each item is then validated in order; a bad item is reported and later valid items continue. Valid additions share one Undo step. A partial result has `Ok=false` and retains `Result` with `Items`, `Total`, `Added`, `Failed`, and the final event metadata. A preview reports `WouldAdd` with `Added=0`. Save the scene after a partial batch if retaining its valid additions.

```bash
unity --json command --project-path "$PROJECT_PATH" event.listeners-add-batch -- \
  --target "$COMPONENT_ID" --eventName onClick \
  --items '[{"target":"123456789","methodName":"OnClick"},{"target":"987654321","methodName":"OnOtherClick"}]' --dryRun true
```

## Edit and copy persistent listeners

`event.listener-remove` removes one persistent index (`index=0` by default). `event.listeners-clear` removes all persistent listeners. `event.listener-state` requires `state` and changes one index (`index=0` by default). These commands accept any `UnityEventBase` member, including generic events. Every edit supports `dryRun=true`, validates before Undo, and returns the observed event metadata. Clear on an already empty event is a no-op.

```bash
unity --json command --project-path "$PROJECT_PATH" event.listener-state -- \
  --target "$COMPONENT_ID" --eventName onClick --state Off --index 0
unity --json command --project-path "$PROJECT_PATH" event.listener-remove -- \
  --target "$COMPONENT_ID" --eventName onClick --index 0
unity --json command --project-path "$PROJECT_PATH" event.listeners-clear -- \
  --target "$COMPONENT_ID" --eventName onClick --dryRun true
```

`event.listeners-copy` appends eligible no-argument persistent callbacks from `sourceTarget`/`sourceEvent` to a parameterless destination `target`/`eventName`. It preserves source order and each copied call state, including `Off`, while leaving destination listeners in place. Typed argument callbacks, null targets, and callbacks that cannot be resolved to the exact public no-argument method are skipped with source index and reason. The source's serialized persistent mode determines eligibility, so an `int` callback is not silently rebound to a same-name no-argument overload. Preview first if the destination listener count matters.

```bash
unity --json command --project-path "$PROJECT_PATH" event.listeners-copy -- \
  --sourceTarget "$SOURCE_COMPONENT_ID" --sourceEvent onClick \
  --target "$DESTINATION_COMPONENT_ID" --eventName onClick --dryRun true
```

## Invoke once

`event.invoke` calls public `UnityEvent.Invoke()` once on a parameterless event. `dryRun=true` validates and previews without calling it. Execution requires `confirm=true`, is not Undoable, and can change arbitrary listener-owned state. In edit mode Unity applies its call-state filtering; use `EditorAndRuntime` for edit-mode callbacks. `RuntimeOnly` callbacks run in Play mode, and `Off` callbacks do not run. Runtime listeners can also fire. A thrown callback returns a typed failure with `Dispatched=true` and exception text; effects from earlier callbacks may remain.

```bash
unity --json command --project-path "$PROJECT_PATH" event.invoke -- \
  --target "$COMPONENT_ID" --eventName onClick --dryRun true
unity --json command --project-path "$PROJECT_PATH" event.invoke -- \
  --target "$COMPONENT_ID" --eventName onClick --confirm true
```

All persistent edits mark the owning component and scene dirty and record prefab-instance property modifications. Reinspect the exact Component after Undo, reload, or scene reopen; persistent event instances may be replaced by Unity during those operations.
