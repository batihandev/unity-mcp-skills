using System;
using System.Collections.Generic;

namespace BatihanDev.UnityCliCommands.Events
{
    [Serializable] public sealed class EventListenerInfo
    {
        public int Index { get; set; }
        public string Target { get; set; }
        public string TargetName { get; set; }
        public string TargetType { get; set; }
        public string MethodName { get; set; }
        public string State { get; set; }
    }
    [Serializable] public sealed class EventInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsNull { get; set; }
        public int PersistentCount { get; set; }
        public List<EventListenerInfo> Listeners { get; set; }
    }
    [Serializable] public sealed class EventInspectResult
    {
        public string Target { get; set; }
        public List<EventInfo> Events { get; set; }
    }
    [Serializable] public sealed class EventEditResult
    {
        public string Target { get; set; }
        public string EventName { get; set; }
        public int Before { get; set; }
        public int After { get; set; }
        public int Changed { get; set; }
        public int Index { get; set; }
        public string State { get; set; }
        public bool DryRun { get; set; }
        public bool Applied { get; set; }
        public EventInfo Event { get; set; }
    }
    [Serializable] public sealed class EventBatchItemResult
    {
        public int Index { get; set; }
        public string Target { get; set; }
        public string MethodName { get; set; }
        public bool Ok { get; set; }
        public string Error { get; set; }
        public int AddedIndex { get; set; }
    }
    [Serializable] public sealed class EventBatchResult
    {
        public string Target { get; set; }
        public string EventName { get; set; }
        public int Total { get; set; }
        public int Added { get; set; }
        public int WouldAdd { get; set; }
        public int Failed { get; set; }
        public bool DryRun { get; set; }
        public List<EventBatchItemResult> Items { get; set; }
        public EventInfo Event { get; set; }
    }
    [Serializable] public sealed class EventCopySkip
    {
        public int SourceIndex { get; set; }
        public string Reason { get; set; }
    }
    [Serializable] public sealed class EventCopyResult
    {
        public string SourceTarget { get; set; }
        public string SourceEvent { get; set; }
        public string Target { get; set; }
        public string EventName { get; set; }
        public int Copied { get; set; }
        public int WouldCopy { get; set; }
        public bool DryRun { get; set; }
        public List<EventCopySkip> Skipped { get; set; }
        public EventInfo Event { get; set; }
    }
    [Serializable] public sealed class EventInvokeResult
    {
        public string Target { get; set; }
        public string EventName { get; set; }
        public bool DryRun { get; set; }
        public bool Dispatched { get; set; }
        public int PersistentCount { get; set; }
        public string Exception { get; set; }
    }
}
