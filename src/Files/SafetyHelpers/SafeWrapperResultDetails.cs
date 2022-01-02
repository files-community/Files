using Files.Enums;
using System;

namespace Files.SafetyHelpers
{
    public sealed class SafeWrapperResultDetails
    {
        public readonly OperationErrorCode errorCode;

        public readonly Exception innerException;

        public readonly string message;

        public SafeWrapperResultDetails(OperationErrorCode errorCode)
            : this(errorCode, null, null)
        {
        }

        public SafeWrapperResultDetails(OperationErrorCode errorCode, Exception innerException)
            : this(errorCode, innerException, null)
        {
        }

        public SafeWrapperResultDetails(OperationErrorCode errorCode, string message)
            : this(errorCode, null, message)
        {
        }

        public SafeWrapperResultDetails(OperationErrorCode errorCode, Exception innerException, string message)
        {
            this.errorCode = errorCode;
            this.innerException = innerException;
            this.message = message;
        }

        public static implicit operator SafeWrapperResultDetails((OperationErrorCode errorCode, Exception innerException) details)
            => new SafeWrapperResultDetails(details.errorCode, details.innerException);

        public static implicit operator SafeWrapperResultDetails((OperationErrorCode errorCode, string message) details)
            => new SafeWrapperResultDetails(details.errorCode, details.message);

        public static implicit operator SafeWrapperResultDetails((OperationErrorCode errorCode, Exception innerException, string message) details)
            => new SafeWrapperResultDetails(details.errorCode, details.innerException, details.message);
    }
}
