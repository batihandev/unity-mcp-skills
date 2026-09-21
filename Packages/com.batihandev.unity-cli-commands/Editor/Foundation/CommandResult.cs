using System;
using System.Collections.Generic;

namespace BatihanDev.UnityCliCommands.Foundation
{
    [Serializable]
    public sealed class CommandResult<T>
    {
        public string Schema { get; set; }
        public bool Ok { get; set; }
        public T Result { get; set; }
        public CommandError Error { get; set; }

        public static CommandResult<T> Success(string schema, T result)
        {
            return new CommandResult<T>
            {
                Schema = schema,
                Ok = true,
                Result = result
            };
        }

        public static CommandResult<T> Failure(
            string schema,
            string code,
            string message,
            Dictionary<string, object> details = null)
        {
            return new CommandResult<T>
            {
                Schema = schema,
                Ok = false,
                Error = new CommandError
                {
                    Schema = "unity.command.error@1",
                    Code = code,
                    Message = message,
                    Details = details
                }
            };
        }

        public static CommandResult<T> Failure(string schema, CommandError error)
        {
            return new CommandResult<T>
            {
                Schema = schema,
                Ok = false,
                Error = error
            };
        }
    }

    [Serializable]
    public sealed class CommandError
    {
        public string Schema { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
        public Dictionary<string, object> Details { get; set; }
    }

    public static class FoundationErrorCode
    {
        public const string UnsupportedUnityVersion = "UNSUPPORTED_UNITY_VERSION";
        public const string RequiredPackageMissing = "REQUIRED_PACKAGE_MISSING";
        public const string PathRequired = "PATH_REQUIRED";
        public const string PathOutsideRoot = "PATH_OUTSIDE_ROOT";
        public const string PathTraversal = "PATH_TRAVERSAL";
        public const string PathRootForbidden = "PATH_ROOT_FORBIDDEN";
        public const string PathReparsePoint = "PATH_REPARSE_POINT";
        public const string FileNotFound = "FILE_NOT_FOUND";
        public const string NotRegularFile = "NOT_REGULAR_FILE";
        public const string CaptureExtensionUnsupported = "CAPTURE_EXTENSION_UNSUPPORTED";
        public const string PathInvalid = "PATH_INVALID";
        public const string PathAccessDenied = "PATH_ACCESS_DENIED";
        public const string PathIoError = "PATH_IO_ERROR";
        public const string PackageNotEmbedded = "PACKAGE_NOT_EMBEDDED";
        public const string PathNotWritable = "PATH_NOT_WRITABLE";
    }
}
