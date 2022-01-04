using System;

namespace Files.Common.SafetyHelpers.ExceptionReporters
{
    public interface ISafeWrapperExceptionReporter
    {
        SafeWrapperResult GetStatusResult(Exception e);

        SafeWrapperResult GetStatusResult(Exception e, Type callerType);
    }
}
