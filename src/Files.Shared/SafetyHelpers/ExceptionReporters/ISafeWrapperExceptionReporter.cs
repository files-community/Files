using System;

namespace Files.Shared.SafetyHelpers.ExceptionReporters
{
    public interface ISafeWrapperExceptionReporter
    {
        SafeWrapperResult GetStatusResult(Exception e);

        SafeWrapperResult GetStatusResult(Exception e, Type callerType);
    }
}
