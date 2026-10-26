using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BatihanDev.UnityCliCommands.Foundation;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityObject = UnityEngine.Object;
using UnityComponent = UnityEngine.Component;

namespace BatihanDev.UnityCliCommands.Events
{
    public static class EventCommands
    {
        private const string InspectSchema = "unity.event.inspect@1";
        private const string EditSchema = "unity.event.edit@1";
        private const string BatchSchema = "unity.event.listeners-add-batch@1";
        private const string CopySchema = "unity.event.listeners-copy@1";
        private const string InvokeSchema = "unity.event.invoke@1";

        [CliCommand("event.inspect", "Inspect public UnityEvent members and persistent listeners on one exact scene Component.", Tags = new[] { "unity-cli-commands", "event" })]
        public static CommandResult<EventInspectResult> Inspect(
            [CliArg("target", "Exact source Component ObjectRef.", Required = true)] string target,
            string eventName = null, bool includeListeners = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<EventInspectResult>.Failure(InspectSchema, compatibility.Error);
            if (!EventResolver.TryComponent(target, out var component, out var error)) return Fail<EventInspectResult>(InspectSchema, "TARGET_INVALID", error);
            List<ResolvedEvent> events;
            if (eventName == null) events = EventResolver.List(component);
            else
            {
                if (!EventResolver.TryEvent(component, eventName, false, out var selected, out error)) return Fail<EventInspectResult>(InspectSchema, "EVENT_INVALID", error);
                events = new List<ResolvedEvent> { selected };
            }
            return CommandResult<EventInspectResult>.Success(InspectSchema, new EventInspectResult
            {
                Target = BatihanDev.UnityCliCommands.Editor.ExactObjectReference.ExactId(component),
                Events = events.Select(item => Describe(item, includeListeners)).ToList()
            });
        }

        [CliCommand("event.listener-add", "Add one validated persistent listener; generic events support static void callbacks.", Tags = new[] { "unity-cli-commands", "event" })]
        public static CommandResult<EventEditResult> ListenerAdd(
            [CliArg("target", "Exact source Component ObjectRef.", Required = true)] string target,
            [CliArg("eventName", "Exact public event member name.", Required = true)] string eventName,
            [CliArg("listenerTarget", "Exact listener Component or GameObject ObjectRef.", Required = true)] string listenerTarget,
            [CliArg("methodName", "Exact public instance void method.", Required = true)] string methodName,
            string argType = "void", string mode = "RuntimeOnly", float floatArg = 0, int intArg = 0,
            string stringArg = null, bool boolArg = false, bool dryRun = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<EventEditResult>.Failure(EditSchema, compatibility.Error);
            if (!TryEdit(target, eventName, out var resolved, out var error)) return Fail<EventEditResult>(EditSchema, "EVENT_INVALID", error);
            if (!(resolved.Value is UnityEvent) && !string.Equals(argType, "void", StringComparison.OrdinalIgnoreCase))
                return Fail<EventEditResult>(EditSchema, "STANDARD_EVENT_REQUIRED", "Generic events accept only argType=void static callbacks.");
            if (!EventResolver.TryState(mode, out var state)) return Fail<EventEditResult>(EditSchema, "MODE_INVALID", "Use Off, RuntimeOnly, or EditorAndRuntime.");
            if (!TryAddPlan(listenerTarget, methodName, argType, floatArg, out var plan, out error)) return Fail<EventEditResult>(EditSchema, "LISTENER_INVALID", error);
            var before = resolved.Value.GetPersistentEventCount();
            if (!dryRun)
            {
                Record(resolved.Component, "Add Event Listener");
                if (!TryEdit(target, eventName, out resolved, out error)) return Fail<EventEditResult>(EditSchema, "EVENT_INVALID", error);
                if (resolved.Value is UnityEvent standard) ApplyAdd(standard, plan, state, floatArg, intArg, stringArg, boolArg);
                else
                {
                    UnityEventTools.AddVoidPersistentListener(resolved.Value, (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), plan.Target, plan.Method));
                    resolved.Value.SetPersistentListenerState(resolved.Value.GetPersistentEventCount() - 1, state);
                }
                EventResolver.Dirty(resolved.Component);
            }
            if (!TryEdit(target, eventName, out resolved, out error)) return Fail<EventEditResult>(EditSchema, "READBACK_FAILED", error);
            var after = resolved.Value.GetPersistentEventCount();
            if (!dryRun && after != before + 1) return Fail<EventEditResult>(EditSchema, "READBACK_MISMATCH", "The persistent listener count did not increase.");
            return CommandResult<EventEditResult>.Success(EditSchema, Edit(resolved, before, after, 1, before, dryRun, state.ToString()));
        }

        [CliCommand("event.listeners-add-batch", "Add ordered parameterless listeners, retaining valid items and reporting each failure.", Tags = new[] { "unity-cli-commands", "event" })]
        public static CommandResult<EventBatchResult> ListenersAddBatch(
            [CliArg("target", "Exact source Component ObjectRef.", Required = true)] string target,
            [CliArg("eventName", "Exact public event member name.", Required = true)] string eventName,
            [CliArg("items", "Strict JSON array of {target,methodName} objects.", Required = true)] string items,
            bool dryRun = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<EventBatchResult>.Failure(BatchSchema, compatibility.Error);
            if (!TryEdit(target, eventName, out var resolved, out var error)) return Fail<EventBatchResult>(BatchSchema, "EVENT_INVALID", error);
            if (!(resolved.Value is UnityEvent)) return Fail<EventBatchResult>(BatchSchema, "STANDARD_EVENT_REQUIRED", "Batch addition requires a parameterless UnityEvent.");
            if (!TryItems(items, out var parsed, out error)) return Fail<EventBatchResult>(BatchSchema, "ITEMS_INVALID", error);
            var result = new EventBatchResult { Target = target, EventName = eventName, Total = parsed.Count, DryRun = dryRun, Items = new List<EventBatchItemResult>() };
            var recorded = false;
            for (var index = 0; index < parsed.Count; index++)
            {
                var item = parsed[index];
                var itemResult = new EventBatchItemResult { Index = index, Target = item.Item1, MethodName = item.Item2, AddedIndex = -1 };
                result.Items.Add(itemResult);
                if (!TryAddPlan(item.Item1, item.Item2, "void", 0, out var plan, out error))
                {
                    itemResult.Error = error;
                    result.Failed++;
                    continue;
                }
                itemResult.Ok = true;
                itemResult.AddedIndex = resolved.Value.GetPersistentEventCount() + (dryRun ? result.WouldAdd : 0);
                if (dryRun) { result.WouldAdd++; continue; }
                if (!recorded)
                {
                    Record(resolved.Component, "Add Event Listeners");
                    recorded = true;
                }
                if (!TryEdit(target, eventName, out resolved, out error))
                {
                    itemResult.Ok = false; itemResult.Error = error; result.Failed++; continue;
                }
                ApplyAdd((UnityEvent)resolved.Value, plan, UnityEventCallState.RuntimeOnly, 0, 0, null, false);
                result.Added++;
            }
            if (recorded) EventResolver.Dirty(resolved.Component);
            if (!TryEdit(target, eventName, out resolved, out error)) return Fail<EventBatchResult>(BatchSchema, "READBACK_FAILED", error);
            result.Event = Describe(resolved, true);
            if (result.Failed == 0) return CommandResult<EventBatchResult>.Success(BatchSchema, result);
            var failure = Fail<EventBatchResult>(BatchSchema, "BATCH_PARTIAL_FAILURE", "One or more listener items failed validation or application.");
            failure.Result = result;
            return failure;
        }

        [CliCommand("event.listener-remove", "Remove one persistent listener by index.", Tags = new[] { "unity-cli-commands", "event" })]
        public static CommandResult<EventEditResult> ListenerRemove(
            [CliArg("target", "Exact source Component ObjectRef.", Required = true)] string target,
            [CliArg("eventName", "Exact public event member name.", Required = true)] string eventName,
            int index = 0, bool dryRun = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<EventEditResult>.Failure(EditSchema, compatibility.Error);
            if (!TryEdit(target, eventName, out var resolved, out var error)) return Fail<EventEditResult>(EditSchema, "EVENT_INVALID", error);
            var before = resolved.Value.GetPersistentEventCount();
            if (index < 0 || index >= before) return Fail<EventEditResult>(EditSchema, "INDEX_INVALID", "Persistent listener index is out of range.");
            if (!dryRun)
            {
                Record(resolved.Component, "Remove Event Listener");
                if (!TryEdit(target, eventName, out resolved, out error)) return Fail<EventEditResult>(EditSchema, "EVENT_INVALID", error);
                UnityEventTools.RemovePersistentListener(resolved.Value, index);
                EventResolver.Dirty(resolved.Component);
            }
            if (!TryEdit(target, eventName, out resolved, out error)) return Fail<EventEditResult>(EditSchema, "READBACK_FAILED", error);
            var after = resolved.Value.GetPersistentEventCount();
            if (!dryRun && after != before - 1) return Fail<EventEditResult>(EditSchema, "READBACK_MISMATCH", "The persistent listener count did not decrease.");
            return CommandResult<EventEditResult>.Success(EditSchema, Edit(resolved, before, after, 1, index, dryRun));
        }

        [CliCommand("event.listeners-clear", "Remove all persistent listeners on one UnityEventBase.", Tags = new[] { "unity-cli-commands", "event" })]
        public static CommandResult<EventEditResult> ListenersClear(
            [CliArg("target", "Exact source Component ObjectRef.", Required = true)] string target,
            [CliArg("eventName", "Exact public event member name.", Required = true)] string eventName,
            bool dryRun = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<EventEditResult>.Failure(EditSchema, compatibility.Error);
            if (!TryEdit(target, eventName, out var resolved, out var error)) return Fail<EventEditResult>(EditSchema, "EVENT_INVALID", error);
            var before = resolved.Value.GetPersistentEventCount();
            if (!dryRun && before > 0)
            {
                Record(resolved.Component, "Clear Event Listeners");
                if (!TryEdit(target, eventName, out resolved, out error)) return Fail<EventEditResult>(EditSchema, "EVENT_INVALID", error);
                for (var index = resolved.Value.GetPersistentEventCount() - 1; index >= 0; index--)
                    UnityEventTools.RemovePersistentListener(resolved.Value, index);
                EventResolver.Dirty(resolved.Component);
            }
            if (!TryEdit(target, eventName, out resolved, out error)) return Fail<EventEditResult>(EditSchema, "READBACK_FAILED", error);
            var after = resolved.Value.GetPersistentEventCount();
            if (!dryRun && after != 0) return Fail<EventEditResult>(EditSchema, "READBACK_MISMATCH", "Persistent listeners remain after clear.");
            return CommandResult<EventEditResult>.Success(EditSchema, Edit(resolved, before, after, before, -1, dryRun));
        }

        [CliCommand("event.listener-state", "Set one persistent listener call state.", Tags = new[] { "unity-cli-commands", "event" })]
        public static CommandResult<EventEditResult> ListenerState(
            [CliArg("target", "Exact source Component ObjectRef.", Required = true)] string target,
            [CliArg("eventName", "Exact public event member name.", Required = true)] string eventName,
            [CliArg("state", "Off, RuntimeOnly, or EditorAndRuntime.", Required = true)] string state,
            int index = 0, bool dryRun = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<EventEditResult>.Failure(EditSchema, compatibility.Error);
            if (!TryEdit(target, eventName, out var resolved, out var error)) return Fail<EventEditResult>(EditSchema, "EVENT_INVALID", error);
            var before = resolved.Value.GetPersistentEventCount();
            if (index < 0 || index >= before) return Fail<EventEditResult>(EditSchema, "INDEX_INVALID", "Persistent listener index is out of range.");
            if (!EventResolver.TryState(state, out var parsed)) return Fail<EventEditResult>(EditSchema, "STATE_INVALID", "Use Off, RuntimeOnly, or EditorAndRuntime.");
            var old = resolved.Value.GetPersistentListenerState(index);
            if (!dryRun && old != parsed)
            {
                Record(resolved.Component, "Set Event Listener State");
                if (!TryEdit(target, eventName, out resolved, out error)) return Fail<EventEditResult>(EditSchema, "EVENT_INVALID", error);
                resolved.Value.SetPersistentListenerState(index, parsed);
                EventResolver.Dirty(resolved.Component);
            }
            if (!TryEdit(target, eventName, out resolved, out error)) return Fail<EventEditResult>(EditSchema, "READBACK_FAILED", error);
            if (!dryRun && resolved.Value.GetPersistentListenerState(index) != parsed) return Fail<EventEditResult>(EditSchema, "READBACK_MISMATCH", "Listener state did not match the request.");
            return CommandResult<EventEditResult>.Success(EditSchema, Edit(resolved, before, resolved.Value.GetPersistentEventCount(), old == parsed ? 0 : 1, index, dryRun, parsed.ToString()));
        }

        [CliCommand("event.listeners-copy", "Append eligible parameterless persistent listeners to one standard UnityEvent.", Tags = new[] { "unity-cli-commands", "event" })]
        public static CommandResult<EventCopyResult> ListenersCopy(
            [CliArg("sourceTarget", "Exact source Component ObjectRef.", Required = true)] string sourceTarget,
            [CliArg("sourceEvent", "Exact public source event member.", Required = true)] string sourceEvent,
            [CliArg("target", "Exact destination Component ObjectRef.", Required = true)] string target,
            [CliArg("eventName", "Exact public destination event member.", Required = true)] string eventName,
            bool dryRun = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<EventCopyResult>.Failure(CopySchema, compatibility.Error);
            if (!TryEdit(sourceTarget, sourceEvent, out var source, out var error)) return Fail<EventCopyResult>(CopySchema, "SOURCE_INVALID", error);
            if (!TryEdit(target, eventName, out var destination, out error)) return Fail<EventCopyResult>(CopySchema, "TARGET_INVALID", error);
            if (!(destination.Value is UnityEvent)) return Fail<EventCopyResult>(CopySchema, "STANDARD_EVENT_REQUIRED", "Copy destination requires a parameterless UnityEvent.");
            var result = new EventCopyResult { SourceTarget = sourceTarget, SourceEvent = sourceEvent, Target = target, EventName = eventName, DryRun = dryRun, Skipped = new List<EventCopySkip>() };
            var plans = new List<Tuple<int, AddPlan, UnityEventCallState>>();
            var count = source.Value.GetPersistentEventCount();
            for (var index = 0; index < count; index++)
            {
                var mode = EventResolver.PersistentMode(source, index);
                if (mode != 1 && (mode != 0 || !(source.Value is UnityEvent)))
                { result.Skipped.Add(new EventCopySkip { SourceIndex = index, Reason = "Typed or unsupported persistent argument mode." }); continue; }
                var objectTarget = source.Value.GetPersistentTarget(index);
                if (objectTarget == null) { result.Skipped.Add(new EventCopySkip { SourceIndex = index, Reason = "Null persistent target." }); continue; }
                var reference = BatihanDev.UnityCliCommands.Editor.ExactObjectReference.ExactId(objectTarget);
                if (!TryAddPlan(reference, source.Value.GetPersistentMethodName(index), "void", 0, out var plan, out error))
                { result.Skipped.Add(new EventCopySkip { SourceIndex = index, Reason = error }); continue; }
                plans.Add(Tuple.Create(index, plan, source.Value.GetPersistentListenerState(index)));
            }
            result.WouldCopy = plans.Count;
            if (!dryRun && plans.Count > 0)
            {
                Record(destination.Component, "Copy Event Listeners");
                if (!TryEdit(target, eventName, out destination, out error)) return Fail<EventCopyResult>(CopySchema, "TARGET_INVALID", error);
                foreach (var plan in plans)
                {
                    ApplyAdd((UnityEvent)destination.Value, plan.Item2, plan.Item3, 0, 0, null, false);
                    result.Copied++;
                }
                EventResolver.Dirty(destination.Component);
            }
            if (!TryEdit(target, eventName, out destination, out error)) return Fail<EventCopyResult>(CopySchema, "READBACK_FAILED", error);
            result.Event = Describe(destination, true);
            return CommandResult<EventCopyResult>.Success(CopySchema, result);
        }

        [CliCommand("event.invoke", "Invoke one parameterless UnityEvent once after explicit confirmation.", Tags = new[] { "unity-cli-commands", "event" })]
        public static CommandResult<EventInvokeResult> Invoke(
            [CliArg("target", "Exact source Component ObjectRef.", Required = true)] string target,
            [CliArg("eventName", "Exact public event member name.", Required = true)] string eventName,
            bool confirm = false, bool dryRun = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<EventInvokeResult>.Failure(InvokeSchema, compatibility.Error);
            if (!EventResolver.TryComponent(target, out var component, out var error) || !EventResolver.TryEvent(component, eventName, false, out var resolved, out error))
                return Fail<EventInvokeResult>(InvokeSchema, "EVENT_INVALID", error);
            if (!(resolved.Value is UnityEvent unityEvent)) return Fail<EventInvokeResult>(InvokeSchema, "STANDARD_EVENT_REQUIRED", "Invoke requires a parameterless UnityEvent.");
            if (!dryRun && !confirm) return Fail<EventInvokeResult>(InvokeSchema, "CONFIRMATION_REQUIRED", "Set confirm=true or dryRun=true.");
            var result = new EventInvokeResult { Target = target, EventName = eventName, DryRun = dryRun, PersistentCount = unityEvent.GetPersistentEventCount() };
            if (dryRun) return CommandResult<EventInvokeResult>.Success(InvokeSchema, result);
            result.Dispatched = true;
            try { unityEvent.Invoke(); }
            catch (Exception exception)
            {
                result.Exception = exception.ToString();
                var failure = Fail<EventInvokeResult>(InvokeSchema, "LISTENER_EXCEPTION", "A listener threw while invoking the event; earlier listener effects may remain.");
                failure.Result = result;
                return failure;
            }
            return CommandResult<EventInvokeResult>.Success(InvokeSchema, result);
        }

        private static bool TryEdit(string target, string eventName, out ResolvedEvent resolved, out string error)
        {
            resolved = null;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                error = "Persistent event listeners can be edited only in Edit mode. Stop Play mode and try again.";
                return false;
            }
            return EventResolver.TryComponent(target, out var component, out error) && EventResolver.TryEvent(component, eventName, true, out resolved, out error);
        }

        private static EventInfo Describe(ResolvedEvent resolved, bool listeners)
        {
            var info = new EventInfo { Name = resolved.Name, Type = resolved.MemberType.FullName,
                IsNull = resolved.Value == null, PersistentCount = resolved.Value == null ? 0 : resolved.Value.GetPersistentEventCount() };
            if (!listeners) return info;
            info.Listeners = new List<EventListenerInfo>();
            for (var index = 0; index < info.PersistentCount; index++)
            {
                var target = resolved.Value.GetPersistentTarget(index);
                info.Listeners.Add(new EventListenerInfo
                {
                    Index = index, Target = BatihanDev.UnityCliCommands.Editor.ExactObjectReference.ExactId(target),
                    TargetName = target == null ? null : target.name, TargetType = target == null ? null : target.GetType().FullName,
                    MethodName = resolved.Value.GetPersistentMethodName(index), State = resolved.Value.GetPersistentListenerState(index).ToString()
                });
            }
            return info;
        }

        private static EventEditResult Edit(ResolvedEvent resolved, int before, int after, int changed, int index, bool dryRun, string state = null) =>
            new EventEditResult
            {
                Target = BatihanDev.UnityCliCommands.Editor.ExactObjectReference.ExactId(resolved.Component), EventName = resolved.Name,
                Before = before, After = after, Changed = changed, Index = index, DryRun = dryRun, Applied = !dryRun && changed > 0,
                State = state, Event = Describe(resolved, true)
            };

        private sealed class AddPlan
        {
            internal UnityObject Target;
            internal MethodInfo Method;
            internal Type Argument;
        }

        private static bool TryAddPlan(string target, string name, string argType, float floatArg, out AddPlan plan, out string error)
        {
            plan = null;
            Type argument;
            switch (argType?.ToLowerInvariant())
            {
                case "void": argument = null; break;
                case "int": argument = typeof(int); break;
                case "float": argument = typeof(float); break;
                case "string": argument = typeof(string); break;
                case "bool": argument = typeof(bool); break;
                default: error = "argType must be void, int, float, string, or bool."; return false;
            }
            if (argument == typeof(float) && (float.IsNaN(floatArg) || float.IsInfinity(floatArg)))
            { error = "floatArg must be finite."; return false; }
            if (!EventResolver.TryListener(target, out var listener, out error)) return false;
            if (!EventResolver.TryMethod(listener, name, argument, out var method, out error)) return false;
            plan = new AddPlan { Target = listener, Method = method, Argument = argument };
            return true;
        }

        private static void ApplyAdd(UnityEvent unityEvent, AddPlan plan, UnityEventCallState state, float floatArg, int intArg, string stringArg, bool boolArg)
        {
            if (plan.Argument == null) UnityEventTools.AddPersistentListener(unityEvent, (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), plan.Target, plan.Method));
            else if (plan.Argument == typeof(int)) UnityEventTools.AddIntPersistentListener(unityEvent, (UnityAction<int>)Delegate.CreateDelegate(typeof(UnityAction<int>), plan.Target, plan.Method), intArg);
            else if (plan.Argument == typeof(float)) UnityEventTools.AddFloatPersistentListener(unityEvent, (UnityAction<float>)Delegate.CreateDelegate(typeof(UnityAction<float>), plan.Target, plan.Method), floatArg);
            else if (plan.Argument == typeof(string)) UnityEventTools.AddStringPersistentListener(unityEvent, (UnityAction<string>)Delegate.CreateDelegate(typeof(UnityAction<string>), plan.Target, plan.Method), stringArg);
            else UnityEventTools.AddBoolPersistentListener(unityEvent, (UnityAction<bool>)Delegate.CreateDelegate(typeof(UnityAction<bool>), plan.Target, plan.Method), boolArg);
            unityEvent.SetPersistentListenerState(unityEvent.GetPersistentEventCount() - 1, state);
        }

        private static bool TryItems(string text, out List<Tuple<string, string>> items, out string error)
        {
            items = null;
            error = "items must be a JSON array of objects containing only nonempty target and methodName strings.";
            try
            {
                if (!(JToken.Parse(text ?? string.Empty) is JArray array)) return false;
                var result = new List<Tuple<string, string>>();
                foreach (var entry in array)
                {
                    if (!(entry is JObject obj) || obj.Properties().Count() != 2 ||
                        obj.Properties().Any(property => property.Name != "target" && property.Name != "methodName") ||
                        obj["target"]?.Type != JTokenType.String || obj["methodName"]?.Type != JTokenType.String ||
                        string.IsNullOrWhiteSpace((string)obj["target"]) || string.IsNullOrWhiteSpace((string)obj["methodName"])) return false;
                    result.Add(Tuple.Create((string)obj["target"], (string)obj["methodName"]));
                }
                items = result;
                error = null;
                return true;
            }
            catch (Exception) { return false; }
        }

        private static void Record(UnityComponent component, string label)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(label);
            Undo.RecordObject(component, label);
        }

        private static CommandResult<T> Fail<T>(string schema, string code, string message) => CommandResult<T>.Failure(schema, code, message);
    }
}
