using Files.Common.SafetyHelpers.ExceptionReporters;
using System;

namespace Files.Common.SafetyHelpers
{
    public class SafeWrapperResult
    {
        public static readonly SafeWrapperResult SUCCESS = new SafeWrapperResult(OperationErrorCode.Success, "Operation completed successfully.");

        public static readonly SafeWrapperResult CANCEL = new SafeWrapperResult(OperationErrorCode.Canceled, "The operation was canceled.");

        public static readonly SafeWrapperResult UNKNOWN_FAIL = new SafeWrapperResult(OperationErrorCode.UnknownFailed, "An unknown error occurred.");

        public string Message { get; }

        public OperationErrorCode ErrorCode { get; }

        public Exception Exception { get; }

        public ISafeWrapperExceptionReporter Reporter { get; }

        public SafeWrapperResult(OperationErrorCode status, Exception innerException)
            : this(status, innerException, null)
        {
        }

        public SafeWrapperResult(OperationErrorCode status, string message)
            : this(status, null, message)
        {
        }

        public SafeWrapperResult(OperationErrorCode status, Exception innerException, string message)
            : this(status, innerException, message, new DefaultSafeWrapperExceptionReporter())
        {
        }

        public SafeWrapperResult(OperationErrorCode status, Exception innerException, string message, ISafeWrapperExceptionReporter reporter)
        {
            this.ErrorCode = status;
            this.Reporter = reporter;
        }

        public static implicit operator OperationErrorCode(SafeWrapperResult wrapperResult)
            => wrapperResult?.ErrorCode ?? OperationErrorCode.InvalidArgument;

        public static implicit operator bool(SafeWrapperResult wrapperResult)
            => (wrapperResult?.ErrorCode ?? OperationErrorCode.UnknownFailed) == OperationErrorCode.Success;

        public static implicit operator SafeWrapperResult(bool result)
            => result ? SafeWrapperResult.SUCCESS : new SafeWrapperResult(OperationErrorCode.UnknownFailed, innerException: null);

        public static implicit operator SafeWrapperResult((OperationErrorCode errorCode, Exception innerException) details)
            => new SafeWrapperResult(details.errorCode, details.innerException);

        public static implicit operator SafeWrapperResult((OperationErrorCode errorCode, string message) details)
            => new SafeWrapperResult(details.errorCode, details.message);

        public static implicit operator SafeWrapperResult((OperationErrorCode errorCode, Exception innerException, string message) details)
            => new SafeWrapperResult(details.errorCode, details.innerException, details.message);

        public static implicit operator SafeWrapperResult((OperationErrorCode errorCode, Exception innerException, string message, ISafeWrapperExceptionReporter reporter) details)
            => new SafeWrapperResult(details.errorCode, details.innerException, details.message, details.reporter);

        public static implicit operator SafeWrapperResult((SafeWrapperResult, ISafeWrapperExceptionReporter reporter) safeWrapper)
            => new SafeWrapperResult(safeWrapper.Item1.ErrorCode, safeWrapper.Item1.Exception, safeWrapper.Item1.Message, safeWrapper.Item2);
    }
}
