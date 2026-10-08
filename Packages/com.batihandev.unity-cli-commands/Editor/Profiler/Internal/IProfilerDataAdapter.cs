using System;
using System.Collections.Generic;

namespace BatihanDev.UnityCliCommands.Profiler.Internal
{
    internal sealed class ProfilerDataUnavailableException : Exception
    {
        public ProfilerDataUnavailableException(string message) : base(message)
        {
        }
    }

    internal interface IProfilerDataAdapter
    {
        bool IsRecording { get; }
        event Action HistoryChanged;
        IReadOnlyList<ProfilerFrameRecord> ReadAllFrames();
        bool LoadProfile(string capturePath);
    }
}
