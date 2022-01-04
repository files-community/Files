using Files.Common.SafetyHelpers.ExceptionReporters;
using System;

namespace Files.Common.SafetyHelpers
{
    public sealed class SafeWrapper<TResult> : SafeWrapperResult
    {
        public TResult Result { get; private set; }

        public SafeWrapper(TResult result, OperationErrorCode errorCode)
            : this(result, errorCode, null)
        {
        }

        public SafeWrapper(TResult result, OperationErrorCode errorCode, string message)
            : this(result, errorCode, null, message)
        {
        }

        public SafeWrapper(TResult result, OperationErrorCode errorCode, Exception innerException, string message)
            : this(result, errorCode, innerException, message, new DefaultSafeWrapperExceptionReporter())
        {
        }

        public SafeWrapper(TResult result, OperationErrorCode errorCode, Exception innerException, string message, ISafeWrapperExceptionReporter reporter)
            : base(errorCode, innerException, message, reporter)
        {
            this.Result = result;
        }

        public static implicit operator TResult(SafeWrapper<TResult> safeWrapper) => safeWrapper.Result;

        public static implicit operator SafeWrapper<TResult>((TResult, SafeWrapperResult) safeWrapper) => new SafeWrapper<TResult>(safeWrapper.Item1, safeWrapper.Item2.ErrorCode, safeWrapper.Item2.Exception, safeWrapper.Item2.Message, safeWrapper.Item2.Reporter);

        public static implicit operator SafeWrapper<TResult>((TResult, SafeWrapperResult, ISafeWrapperExceptionReporter reporter) safeWrapper) => new SafeWrapper<TResult>(safeWrapper.Item1, safeWrapper.Item2.ErrorCode, safeWrapper.Item2.Exception, safeWrapper.Item2.Message, safeWrapper.Item3);

        public static implicit operator SafeWrapper<TResult>((SafeWrapper<TResult>, ISafeWrapperExceptionReporter reporter) safeWrapper) => new SafeWrapper<TResult>(safeWrapper.Item1.Result, safeWrapper.Item1.ErrorCode, safeWrapper.Item1.Exception, safeWrapper.Item1.Message, safeWrapper.Item2);
    }
}
