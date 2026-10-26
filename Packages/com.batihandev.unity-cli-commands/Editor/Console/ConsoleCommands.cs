using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BatihanDev.UnityCliCommands.Foundation;
using Unity.Pipeline.Commands;

namespace BatihanDev.UnityCliCommands.Console
{
    public static class ConsoleCommands
    {
        private const string SettingsSchema = "unity.console.settings@1";
        private const string EntriesSchema = "unity.console.entries@1";

        [CliCommand("console.settings", "Read or guardedly patch effective Unity Console flags.",
            Tags = new[] { "unity-cli-commands", "console" })]
        public static CommandResult<ConsoleSettingsResult> Settings(
            [CliArg("collapse", "Optional Collapse flag value.")] bool? collapse = null,
            [CliArg("clearOnPlay", "Optional Clear on Play flag value.")] bool? clearOnPlay = null,
            [CliArg("errorPause", "Optional Error Pause flag value.")] bool? errorPause = null,
            [CliArg("dryRun", "Preview without writing; wins over confirmation.")] bool dryRun = false,
            [CliArg("confirm", "Apply the supplied patch.")] bool confirm = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok)
                return CommandResult<ConsoleSettingsResult>.Failure(SettingsSchema, compatibility.Error);
            try
            {
                var adapter = ConsoleReflectionAdapter.Create();
                var beforeFlags = adapter.ReadFlags();
                var before = adapter.Snapshot(beforeFlags);
                var proposedFlags = adapter.Patch(beforeFlags, collapse, clearOnPlay, errorPause);
                var proposed = adapter.Snapshot(proposedFlags);
                var hasPatch = collapse.HasValue || clearOnPlay.HasValue || errorPause.HasValue;
                if (hasPatch && !dryRun && !confirm)
                    return CommandResult<ConsoleSettingsResult>.Failure(SettingsSchema,
                        "CONFIRMATION_REQUIRED", "Console setting changes require confirm=true or dryRun=true.");

                if (hasPatch && !dryRun)
                    adapter.WriteFlags(proposedFlags);
                var after = adapter.Snapshot(adapter.ReadFlags());
                if (hasPatch && !dryRun && !after.Equals(proposed))
                    return CommandResult<ConsoleSettingsResult>.Failure(SettingsSchema,
                        "READBACK_MISMATCH", "The effective Console flags did not match the requested patch.");

                return CommandResult<ConsoleSettingsResult>.Success(SettingsSchema, new ConsoleSettingsResult
                {
                    Before = before,
                    Proposed = proposed,
                    After = after,
                    Applied = hasPatch && !dryRun,
                    DryRun = dryRun,
                    Undoable = false
                });
            }
            catch (Exception exception)
            {
                return CommandResult<ConsoleSettingsResult>.Failure(SettingsSchema,
                    "CONSOLE_SETTINGS_UNAVAILABLE", "The effective Unity Console flags are unavailable.",
                    new Dictionary<string, object> { ["exceptionType"] = exception.GetType().Name });
            }
        }

        [CliCommand("console.entries", "Read ordered entries from the live Unity Editor Console.",
            Tags = new[] { "unity-cli-commands", "console" })]
        public static CommandResult<ConsoleEntriesResult> Entries(
            [CliArg("limit", "Entry limit from 0 through 1000.")] int limit = 1000)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok)
                return CommandResult<ConsoleEntriesResult>.Failure(EntriesSchema, compatibility.Error);
            if (limit < 0 || limit > 1000)
                return CommandResult<ConsoleEntriesResult>.Failure(EntriesSchema,
                    "LIMIT_OUT_OF_RANGE", "limit must be from 0 through 1000.");
            try
            {
                return CommandResult<ConsoleEntriesResult>.Success(EntriesSchema,
                    ConsoleReflectionAdapter.Create().ReadEntries(limit));
            }
            catch (Exception exception)
            {
                return CommandResult<ConsoleEntriesResult>.Failure(EntriesSchema,
                    "CONSOLE_ENTRIES_UNAVAILABLE", "The live Unity Editor Console entries are unavailable.",
                    new Dictionary<string, object> { ["exceptionType"] = exception.GetType().Name });
            }
        }
    }

    internal sealed class ConsoleReflectionAdapter
    {
        private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly Type _entriesType;
        private readonly Type _entryType;
        private readonly PropertyInfo _flags;
        private readonly MethodInfo _start;
        private readonly MethodInfo _end;
        private readonly MethodInfo _get;
        private readonly FieldInfo _message;
        private readonly FieldInfo _mode;
        private readonly int _collapse;
        private readonly int _clearOnPlay;
        private readonly int _errorPause;
        private readonly int _warningModes;
        private readonly int _errorModes;

        private ConsoleReflectionAdapter(Type entriesType, Type entryType)
        {
            _entriesType = entriesType;
            _entryType = entryType;
            _flags = entriesType.GetProperty("consoleFlags", StaticFlags)
                ?? throw new MissingMemberException(entriesType.FullName, "consoleFlags");
            _start = ExactMethod(entriesType, "StartGettingEntries", Type.EmptyTypes);
            _end = ExactMethod(entriesType, "EndGettingEntries", Type.EmptyTypes);
            _get = entriesType.GetMethods(StaticFlags).Single(method => method.Name == "GetEntryInternal" &&
                method.GetParameters().Length == 2 && method.GetParameters()[0].ParameterType == typeof(int));
            _message = entryType.GetField("message", InstanceFlags)
                ?? throw new MissingFieldException(entryType.FullName, "message");
            _mode = entryType.GetField("mode", InstanceFlags)
                ?? throw new MissingFieldException(entryType.FullName, "mode");
            var flagType = Type.GetType("UnityEditor.ConsoleWindow+ConsoleFlags, UnityEditor", true);
            _collapse = Convert.ToInt32(Enum.Parse(flagType, "Collapse"));
            _clearOnPlay = Convert.ToInt32(Enum.Parse(flagType, "ClearOnPlay"));
            _errorPause = Convert.ToInt32(Enum.Parse(flagType, "ErrorPause"));
            var messageFlagType = Type.GetType("UnityEditor.LogMessageFlags, UnityEditor.CoreModule", true);
            _warningModes = Combine(messageFlagType,
                "kAssetImportWarning", "kScriptingWarning", "kScriptCompileWarning");
            _errorModes = Combine(messageFlagType,
                "kError", "kAssert", "kFatal", "kAssetImportError", "kScriptingError",
                "kScriptCompileError", "kScriptingException", "kScriptingAssertion");
        }

        public static ConsoleReflectionAdapter Create()
        {
            var entriesType = Type.GetType("UnityEditor.LogEntries, UnityEditor", true);
            var entryType = Type.GetType("UnityEditor.LogEntry, UnityEditor", true);
            return new ConsoleReflectionAdapter(entriesType, entryType);
        }

        public int ReadFlags() => Convert.ToInt32(_flags.GetValue(null));

        public void WriteFlags(int flags) => _flags.SetValue(null, flags);

        public ConsoleSettingsSnapshot Snapshot(int flags) => new ConsoleSettingsSnapshot
        {
            Collapse = (flags & _collapse) != 0,
            ClearOnPlay = (flags & _clearOnPlay) != 0,
            ErrorPause = (flags & _errorPause) != 0
        };

        public int Patch(int flags, bool? collapse, bool? clearOnPlay, bool? errorPause)
        {
            flags = PatchFlag(flags, _collapse, collapse);
            flags = PatchFlag(flags, _clearOnPlay, clearOnPlay);
            return PatchFlag(flags, _errorPause, errorPause);
        }

        public ConsoleEntriesResult ReadEntries(int limit)
        {
            var total = Convert.ToInt32(_start.Invoke(null, null));
            var count = Math.Min(total, limit);
            var entries = new List<ConsoleEntryResult>(count);
            try
            {
                for (var index = 0; index < count; index++)
                {
                    var entry = Activator.CreateInstance(_entryType);
                    var args = new[] { (object)index, entry };
                    if (!(bool)_get.Invoke(null, args))
                        throw new InvalidOperationException($"Unity failed to read Console entry {index}.");
                    entry = args[1];
                    var mode = Convert.ToInt32(_mode.GetValue(entry));
                    entries.Add(new ConsoleEntryResult
                    {
                        Index = index,
                        Message = (string)_message.GetValue(entry) ?? string.Empty,
                        Mode = mode,
                        Severity = (mode & _errorModes) != 0 ? "Error" :
                            (mode & _warningModes) != 0 ? "Warning" : "Log"
                    });
                }
            }
            finally
            {
                _end.Invoke(null, null);
            }
            return new ConsoleEntriesResult
            {
                Source = "editor-console",
                Collapse = Snapshot(ReadFlags()).Collapse,
                TotalAvailable = total,
                Count = entries.Count,
                Truncated = total > entries.Count,
                Entries = entries
            };
        }

        private static int PatchFlag(int value, int flag, bool? enabled) => !enabled.HasValue
            ? value
            : enabled.Value ? value | flag : value & ~flag;

        private static MethodInfo ExactMethod(Type type, string name, Type[] parameters) =>
            type.GetMethod(name, StaticFlags, null, parameters, null)
            ?? throw new MissingMethodException(type.FullName, name);

        private static int Combine(Type enumType, params string[] names) =>
            names.Aggregate(0, (value, name) => value | Convert.ToInt32(Enum.Parse(enumType, name)));
    }

    [Serializable]
    public sealed class ConsoleSettingsResult
    {
        public ConsoleSettingsSnapshot Before { get; set; }
        public ConsoleSettingsSnapshot Proposed { get; set; }
        public ConsoleSettingsSnapshot After { get; set; }
        public bool Applied { get; set; }
        public bool DryRun { get; set; }
        public bool Undoable { get; set; }
    }

    [Serializable]
    public sealed class ConsoleSettingsSnapshot : IEquatable<ConsoleSettingsSnapshot>
    {
        public bool Collapse { get; set; }
        public bool ClearOnPlay { get; set; }
        public bool ErrorPause { get; set; }
        public bool Equals(ConsoleSettingsSnapshot other) => other != null && Collapse == other.Collapse &&
            ClearOnPlay == other.ClearOnPlay && ErrorPause == other.ErrorPause;
    }

    [Serializable]
    public sealed class ConsoleEntriesResult
    {
        public string Source { get; set; }
        public bool Collapse { get; set; }
        public int TotalAvailable { get; set; }
        public int Count { get; set; }
        public bool Truncated { get; set; }
        public List<ConsoleEntryResult> Entries { get; set; }
    }

    [Serializable]
    public sealed class ConsoleEntryResult
    {
        public int Index { get; set; }
        public string Message { get; set; }
        public int Mode { get; set; }
        public string Severity { get; set; }
    }
}
