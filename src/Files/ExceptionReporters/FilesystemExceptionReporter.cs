using Files.Common.SafetyHelpers;
using Files.Common.SafetyHelpers.ExceptionReporters;
using Files.Filesystem;
using System;
using System.IO;
using Windows.Storage;

namespace Files.Filesystem //namespace Files.SafetyHelpers.ExceptionReporters
{
    public static class FilesystemTasks2
    {
        public static SafeWrapperResult<T> Get<T>() => SafeWrapperRoutines.SetReporter<T>(typeof(FilesystemExceptionReporter));

        public static SafeWrapperResult Get() => SafeWrapperRoutines.SetReporter(typeof(FilesystemExceptionReporter));
    }

    public class FilesystemExceptionReporter : ISafeWrapperExceptionReporter
    {
        public static ISafeWrapperExceptionReporter DefaultExceptionReporter = new FilesystemExceptionReporter();

        public SafeWrapperResult GetStatusResult(Exception e)
        {
            return GetStatusResult(e, null);
        }

        public SafeWrapperResult GetStatusResult(Exception ex, Type callerType)
        {
            if (ex is UnauthorizedAccessException)
            {
                return (OperationErrorCode.AccessUnauthorized, ex, "Access is unauthorized.");
            }
            else if (ex is FileNotFoundException // Item was deleted
                || ex is System.Runtime.InteropServices.COMException // Item's drive was ejected
                || (uint)ex.HResult == 0x8007000F) // The system cannot find the drive specified
            {
                return (OperationErrorCode.NotFound, ex, "The item was not found.");
            }
            else if (ex is IOException || ex is FileLoadException)
            {
                return (OperationErrorCode.InUse, ex, "The resource is in use.");
            }
            else if (ex is PathTooLongException)
            {
                return (OperationErrorCode.NameTooLong, ex, "Path is too long.");
            }
            else if (ex is ArgumentException) // Item was invalid
            {
                return (typeof(IStorageFolder).IsAssignableFrom(callerType) || callerType == typeof(StorageFolderWithPath)) ?
                    (OperationErrorCode.NotAFolder, ex, "The item is not a folder.") : (OperationErrorCode.NotAFile, ex, "The item is not a file");
            }
            else if ((uint)ex.HResult == 0x800700B7)
            {
                return (OperationErrorCode.AlreadyExists, ex, "The item already exists.");
            }
            else if ((uint)ex.HResult == 0x80071779)
            {
                return (OperationErrorCode.ReadOnly, ex, "The object is read-only.");
            }
            else if ((uint)ex.HResult == 0x800700A1 // The specified path is invalid (usually an mtp device was disconnected)
                || (uint)ex.HResult == 0x8007016A // The cloud file provider is not running
                || (uint)ex.HResult == 0x8000000A) // The data necessary to complete this operation is not yet available)
            {
                return (SafeWrapperResult.UNKNOWN_FAIL.ErrorCode, ex);
            }
            else
            {
                return (SafeWrapperResult.UNKNOWN_FAIL.ErrorCode, ex);
            }
        }
    }
}
