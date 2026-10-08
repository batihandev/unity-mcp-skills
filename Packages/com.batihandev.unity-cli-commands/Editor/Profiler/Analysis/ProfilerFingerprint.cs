using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BatihanDev.UnityCliCommands.Profiler.Internal;

namespace BatihanDev.UnityCliCommands.Profiler.Analysis
{
    public static class ProfilerFingerprint
    {
        public static string Compute(IReadOnlyList<ProfilerFrameRecord> frames)
        {
            byte[] encoded;
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                {
                    var orderedFrames = frames.OrderBy(value => value.FrameIndex).ToList();
                    writer.Write(orderedFrames.Count);
                    foreach (var frame in orderedFrames)
                    {
                        writer.Write(frame.FrameIndex);
                        writer.Write(frame.FrameStartTimeNs);
                        writer.Write(frame.FrameTimeNs);
                        var orderedThreads = frame.Threads.OrderBy(value => value.ThreadIndex).ToList();
                        writer.Write(orderedThreads.Count);
                        foreach (var thread in orderedThreads)
                        {
                            var root = thread.Samples.Where(value => !value.ParentItemId.HasValue)
                                .OrderBy(value => value.ItemId).FirstOrDefault();
                            writer.Write(thread.ThreadIndex);
                            writer.Write(thread.ThreadId);
                            WriteString(writer, thread.ThreadGroupName);
                            WriteString(writer, thread.ThreadName);
                            writer.Write(thread.Samples.Count);
                            writer.Write(root != null);
                            if (root != null)
                            {
                                writer.Write(root.ItemId);
                                writer.Write(root.StartTimeNs);
                                writer.Write(root.TotalTimeNs);
                            }
                        }
                    }
                }
                encoded = stream.ToArray();
            }
            using (var sha = SHA256.Create())
                return Convert.ToBase64String(sha.ComputeHash(encoded))
                    .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static void WriteString(BinaryWriter writer, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }
    }
}
