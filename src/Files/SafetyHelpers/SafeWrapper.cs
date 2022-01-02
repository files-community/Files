using Files.Enums;
using System;

namespace Files.SafetyHelpers
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
            : this(result, new SafeWrapperResultDetails(errorCode, innerException, message))
        {
            this.Result = result;
        }

        public SafeWrapper(TResult result, SafeWrapperResultDetails details)
            : base(details)
        {
            this.Result = result;
        }

        public static implicit operator TResult(SafeWrapper<TResult> safeWrapper) => safeWrapper.Result;

        public static implicit operator SafeWrapper<TResult>((TResult, SafeWrapperResult) safeWrapper) => new SafeWrapper<TResult>(safeWrapper.Item1, safeWrapper.Item2.Details);
    }
}
