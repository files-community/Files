using System;

namespace Files.Shared.SafetyHelpers.ExceptionReporters
{
    public class DefaultSafeWrapperExceptionReporter : ISafeWrapperExceptionReporter
    {
        public SafeWrapperResult GetStatusResult(Exception e)
        {
            return GetStatusResult(e, null);
        }

        public SafeWrapperResult GetStatusResult(Exception e, Type callerType)
        {
            return (SafeWrapperResult.UNKNOWN_FAIL.ErrorCode, e);
        }
    }
}
