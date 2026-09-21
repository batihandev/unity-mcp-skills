using System;
using System.Security.Cryptography;
using System.Text;
using BatihanDev.UnityCliCommands.Foundation;
using BatihanDev.UnityCliCommands.Profiler.Internal;
using BatihanDev.UnityCliCommands.Profiler.Schemas;

namespace BatihanDev.UnityCliCommands.Profiler.Analysis
{
    public static class ProfilerOpaqueId
    {
        public static string Encode(ProfilerOpaqueIdPayload payload)
        {
            var body = string.Join(":", payload.Version, payload.HistoryEpoch,
                (int)payload.ViewKind, payload.FrameIndex, payload.ThreadIndex, payload.ItemId);
            var checksum = Checksum(body);
            return Base64Url(Encoding.UTF8.GetBytes(body + ":" + checksum));
        }

        public static CommandResult<ProfilerOpaqueIdPayload> Decode(
            string encoded,
            long expectedEpoch,
            ProfilerViewKind expectedKind)
        {
            try
            {
                var text = Encoding.UTF8.GetString(FromBase64Url(encoded));
                var parts = text.Split(':');
                if (parts.Length != 7 || !FixedEquals(parts[6], Checksum(string.Join(":", parts, 0, 6))))
                    return Failure(ProfilerErrorCode.SampleIdInvalid);
                var payload = new ProfilerOpaqueIdPayload(
                    int.Parse(parts[0]), long.Parse(parts[1]), (ProfilerViewKind)int.Parse(parts[2]),
                    int.Parse(parts[3]), int.Parse(parts[4]), int.Parse(parts[5]));
                if (payload.Version != 1)
                    return Failure(ProfilerErrorCode.SampleIdInvalid);
                if (payload.HistoryEpoch != expectedEpoch)
                    return Failure(ProfilerErrorCode.SampleIdStale);
                if (payload.ViewKind != expectedKind)
                    return Failure(ProfilerErrorCode.SampleIdKindMismatch);
                return CommandResult<ProfilerOpaqueIdPayload>.Success(ProfilerSchema.OpaqueId, payload);
            }
            catch (Exception)
            {
                return Failure(ProfilerErrorCode.SampleIdInvalid);
            }
        }

        private static CommandResult<ProfilerOpaqueIdPayload> Failure(string code)
        {
            return CommandResult<ProfilerOpaqueIdPayload>.Failure(
                ProfilerSchema.OpaqueId, code, "The profiler identifier is invalid for this history or view.");
        }

        private static string Checksum(string value)
        {
            using (var sha = SHA256.Create())
                return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(value)))
                    .TrimEnd('=').Replace('+', '-').Replace('/', '_').Substring(0, 11);
        }

        private static bool FixedEquals(string left, string right)
        {
            if (left.Length != right.Length) return false;
            var difference = 0;
            for (var i = 0; i < left.Length; i++) difference |= left[i] ^ right[i];
            return difference == 0;
        }

        private static string Base64Url(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static byte[] FromBase64Url(string value)
        {
            var padded = value.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }
            return Convert.FromBase64String(padded);
        }
    }
}
