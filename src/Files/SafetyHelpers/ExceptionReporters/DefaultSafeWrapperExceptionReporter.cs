using Files.Enums;
using Files.Filesystem;
using System;
using System.IO;
using Windows.Storage;

namespace Files.SafetyHelpers.ExceptionReporters
{
    public class DefaultSafeWrapperExceptionReporter : ISafeWrapperExceptionReporter
    {
        public static ISafeWrapperExceptionReporter DefaultExceptionReporter = new DefaultSafeWrapperExceptionReporter();

        public SafeWrapperResultDetails GetStatusResult(Exception e)
        {
            return GetStatusResult(e, null);
        }

        public SafeWrapperResultDetails GetStatusResult(Exception ex, Type callerType)
        {
            if (ex is UnauthorizedAccessException)
            {
                return (OperationErrorCode.AccessUnauthorized, "Access is unauthorized.");
            }
            else if (ex is FileNotFoundException // Item was deleted
                || ex is System.Runtime.InteropServices.COMException // Item's drive was ejected
                || (uint)ex.HResult == 0x8007000F) // The system cannot find the drive specified
            {
                return (OperationErrorCode.NotFound, "The item was not found.");
            }
            else if (ex is IOException || ex is FileLoadException)
            {
                return (OperationErrorCode.InUse, "The resource is in use.");
            }
            else if (ex is PathTooLongException)
            {
                return (OperationErrorCode.NameTooLong, "Path is too long.");
            }
            else if (ex is ArgumentException) // Item was invalid
            {
                return (typeof(IStorageFolder).IsAssignableFrom(callerType) || callerType == typeof(StorageFolderWithPath)) ?
                    (OperationErrorCode.NotAFolder, "The item is not a folder.") : (OperationErrorCode.NotAFile, "The item is not a file");
            }
            else if ((uint)ex.HResult == 0x800700B7)
            {
                return (OperationErrorCode.AlreadyExists, "The item already exists.");
            }
            else if ((uint)ex.HResult == 0x80071779)
            {
                return (OperationErrorCode.ReadOnly, "The object is read-only.");
            }
            else if ((uint)ex.HResult == 0x800700A1 // The specified path is invalid (usually an mtp device was disconnected)
                || (uint)ex.HResult == 0x8007016A // The cloud file provider is not running
                || (uint)ex.HResult == 0x8000000A) // The data necessary to complete this operation is not yet available)
            {
                return SafeWrapperResult.UNKNOWN_FAIL.Details;
            }
            else
            {
                return SafeWrapperResult.UNKNOWN_FAIL.Details;
            }
        }
    }
}
