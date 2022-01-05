using Files.Common.SafetyHelpers.ExceptionReporters;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Files.Common.SafetyHelpers
{
    public static class SafeWrapperRoutines
    {
        private static readonly SafeWrapperResult NullFunctionDelegateResult = new SafeWrapperResult(OperationErrorCode.InvalidArgument, new ArgumentException(), "Passed-in function delegate is null");

        #region SafeWrap

        public static SafeWrapper<T> SafeWrap<T>(Func<T> func)
        {
            if (!AssertNotNull(func)) return new SafeWrapper<T>(default(T), NullFunctionDelegateResult);

            var reporter = TryGetReporter(null);

            try
            {
                return (func.Invoke(), SafeWrapperResult.SUCCESS, reporter);
            }
            catch (Exception e)
            {
                return (default(T), reporter.GetStatusResult(e, typeof(T)), reporter);
            }
        }

        public static SafeWrapperResult SafeWrap(Action action)
        {
            if (!AssertNotNull(action)) return NullFunctionDelegateResult;

            var reporter = TryGetReporter(null);

            try
            {
                action.Invoke();
                return (SafeWrapperResult.SUCCESS, reporter);
            }
            catch (Exception e)
            {
                return (reporter.GetStatusResult(e), reporter);
            }
        }

        public static SafeWrapper<T> SafeWrap<T>(this SafeWrapperResult wrapped, Func<T> func)
        {
            if (!AssertNotNull(func)) return new SafeWrapper<T>(default(T), NullFunctionDelegateResult);

            var reporter = TryGetReporter(wrapped);

            try
            {
                return (func.Invoke(), SafeWrapperResult.SUCCESS, reporter);
            }
            catch (Exception e)
            {
                return (default(T), reporter.GetStatusResult(e, typeof(T)), reporter);
            }
        }

        public static SafeWrapperResult SafeWrap(this SafeWrapperResult wrapped, Action action)
        {
            if (!AssertNotNull(action)) return NullFunctionDelegateResult;

            var reporter = TryGetReporter(wrapped);

            try
            {
                action.Invoke();
                return (SafeWrapperResult.SUCCESS, reporter);
            }
            catch (Exception e)
            {
                return (reporter.GetStatusResult(e), reporter);
            }
        }

        #endregion

        #region SafeWrapAsync

        public static async Task<SafeWrapper<T>> SafeWrapAsync<T>(Func<Task<T>> func)
        {
            if (!AssertNotNull(func)) return new SafeWrapper<T>(default(T), NullFunctionDelegateResult);

            var reporter = TryGetReporter(null);

            try
            {
                return (await func.Invoke(), SafeWrapperResult.SUCCESS, reporter);
            }
            catch (Exception e)
            {
                return (default(T), reporter.GetStatusResult(e, typeof(T)), reporter);
            }
        }

        public static async Task<SafeWrapperResult> SafeWrapAsync(Func<Task> func)
        {
            if (!AssertNotNull(func)) return NullFunctionDelegateResult;

            var reporter = TryGetReporter(null);

            try
            {
                await func.Invoke();
                return (SafeWrapperResult.SUCCESS, reporter);
            }
            catch (Exception e)
            {
                return (reporter.GetStatusResult(e), reporter);
            }
        }

        public static async Task<SafeWrapper<T>> SafeWrapAsync<T>(this SafeWrapperResult wrapped, Func<Task<T>> func)
        {
            if (!AssertNotNull(func)) return new SafeWrapper<T>(default(T), NullFunctionDelegateResult);

            var reporter = TryGetReporter(wrapped);

            try
            {
                return (await func.Invoke(), SafeWrapperResult.SUCCESS, reporter);
            }
            catch (Exception e)
            {
                return (default(T), reporter.GetStatusResult(e, typeof(T)), reporter);
            }
        }

        public static async Task<SafeWrapperResult> SafeWrapAsync(this SafeWrapperResult wrapped, Func<Task> func)
        {
            if (!AssertNotNull(func)) return NullFunctionDelegateResult;

            var reporter = TryGetReporter(wrapped);

            try
            {
                await func.Invoke();
                return (SafeWrapperResult.SUCCESS, reporter);
            }
            catch (Exception e)
            {
                return (reporter.GetStatusResult(e), reporter);
            }
        }

        #endregion

        #region OnSuccess

        public static SafeWrapper<T> OnSuccess<T>(this SafeWrapper<T> wrapped, Action<T> func)
        {
            if (!AssertNotNull(wrapped, func)) return new SafeWrapper<T>(default(T), NullFunctionDelegateResult);

            SafeWrapper<T> result = wrapped;

            var reporter = TryGetReporter(result);

            if (result)
            {
                result.SafeWrap(() => func(wrapped.Result));
            }

            return (result, reporter);
        }

        public static SafeWrapper<T2> OnSuccess<T1, T2>(this SafeWrapper<T1> wrapped, Func<T1, T2> func)
        {
            if (!AssertNotNull(wrapped, func)) return new SafeWrapper<T2>(default(T2), NullFunctionDelegateResult);

            SafeWrapper<T1> result = wrapped;

            var reporter = TryGetReporter(result);

            if (result)
            {
                return result.SafeWrap(() => func(wrapped.Result));
            }

            return (default(T2), result, reporter);
        }

        public static SafeWrapper<T> OnSuccess<T>(this SafeWrapper<T> wrapped, Func<T, T> func)
        {
            if (!AssertNotNull(wrapped, func)) return new SafeWrapper<T>(default(T), NullFunctionDelegateResult);

            SafeWrapper<T> result = wrapped;

            var reporter = TryGetReporter(result);

            if (result)
            {
                return result.SafeWrap(() => func(wrapped.Result));
            }

            return (result, reporter);
        }

        public static SafeWrapperResult OnSuccess(this SafeWrapperResult wrapped, Action action)
        {
            if (!AssertNotNull(wrapped, action)) return NullFunctionDelegateResult;

            SafeWrapperResult result = wrapped;

            var reporter = TryGetReporter(result);

            if (result)
            {
                return result.SafeWrap(() => action());
            }

            return (result, reporter);
        }

        public static SafeWrapperResult OnSuccess(this SafeWrapperResult wrapped, Func<SafeWrapperResult> func)
        {
            if (!AssertNotNull(wrapped, func)) return NullFunctionDelegateResult;

            SafeWrapperResult result = wrapped;

            var reporter = TryGetReporter(result);

            if (result)
            {
                return result.SafeWrap(() => func());
            }

            return (result, reporter);
        }

        #endregion

        #region OnSuccessAsync

        public static async Task<SafeWrapper<T>> OnSuccessAsync<T>(this Task<SafeWrapper<T>> wrapped, Action<T> func)
        {
            if (!AssertNotNull(wrapped, func)) return new SafeWrapper<T>(default(T), NullFunctionDelegateResult);

            SafeWrapper<T> result = await wrapped;

            var reporter = TryGetReporter(result);

            if (result)
            {
                result.SafeWrap(() => func(result.Result));
            }

            return (result, reporter);
        }

        public static async Task<SafeWrapper<T>> OnSuccessAsync<T>(this Task<SafeWrapper<T>> wrapped, Func<T, Task> func)
        {
            if (!AssertNotNull(wrapped, func)) return new SafeWrapper<T>(default(T), NullFunctionDelegateResult);

            SafeWrapper<T> result = await wrapped;

            var reporter = TryGetReporter(result);

            if (result)
            {
                await result.SafeWrapAsync(() => func(result.Result));
            }

            return (result, reporter);
        }

        public static async Task<SafeWrapper<T2>> OnSuccessAsync<T1, T2>(this Task<SafeWrapper<T1>> wrapped, Func<T1, Task<T2>> func)
        {
            if (!AssertNotNull(wrapped, func)) return new SafeWrapper<T2>(default(T2), NullFunctionDelegateResult);

            SafeWrapper<T1> result = await wrapped;

            var reporter = TryGetReporter(result);

            if (result)
            {
                return await result.SafeWrapAsync(() => func(result.Result));
            }

            return (default(T2), result, reporter);
        }

        public static async Task<SafeWrapper<T>> OnSuccessAsync<T>(this Task<SafeWrapper<T>> wrapped, Func<T, Task<T>> func)
        {
            if (!AssertNotNull(wrapped, func)) return new SafeWrapper<T>(default(T), NullFunctionDelegateResult);

            SafeWrapper<T> result = await wrapped;

            var reporter = TryGetReporter(result);

            if (result)
            {
                return await result.SafeWrapAsync(() => func(result.Result));
            }

            return (result, reporter);
        }

        public static async Task<SafeWrapperResult> OnSuccessAsync(this Task<SafeWrapperResult> wrapped, Action func)
        {
            if (!AssertNotNull(wrapped, func)) return NullFunctionDelegateResult;

            SafeWrapperResult result = await wrapped;

            var reporter = TryGetReporter(result);

            if (result)
            {
                return result.SafeWrap(() => func());
            }

            return (result, reporter);
        }

        public static async Task<SafeWrapperResult> OnSuccessAsync(this Task<SafeWrapperResult> wrapped, Func<Task> func)
        {
            if (!AssertNotNull(wrapped, func)) return NullFunctionDelegateResult;

            SafeWrapperResult result = await wrapped;

            var reporter = TryGetReporter(result);

            if (result)
            {
                return await result.SafeWrapAsync(() => func());
            }

            return (result, reporter);
        }

        public static async Task<SafeWrapperResult> OnSuccessAsync<T>(this Task<SafeWrapperResult> wrapped, Func<SafeWrapperResult> func)
        {
            if (!AssertNotNull(wrapped, func)) return new SafeWrapper<T>(default(T), NullFunctionDelegateResult);

            SafeWrapperResult result = await wrapped;

            var reporter = TryGetReporter(result);

            if (result)
            {
                return result.SafeWrap(() => func());
            }

            return (result, reporter);
        }

        public static async Task<SafeWrapper<T>> OnSuccessAsync<T>(this SafeWrapper<T> wrapped, Func<T, Task> func)
        {
            if (!AssertNotNull(wrapped, func)) return new SafeWrapper<T>(default(T), NullFunctionDelegateResult);

            SafeWrapper<T> result = wrapped;

            var reporter = TryGetReporter(result);

            if (result)
            {
                await result.SafeWrapAsync(() => func(result.Result));
            }

            return (result, reporter);
        }

        public static async Task<SafeWrapper<T2>> OnSuccessAsync<T1, T2>(this SafeWrapper<T1> wrapped, Func<T1, Task<T2>> func)
        {
            if (!AssertNotNull(wrapped, func)) return new SafeWrapper<T2>(default(T2), NullFunctionDelegateResult);

            SafeWrapper<T1> result = wrapped;

            var reporter = TryGetReporter(result);

            if (result)
            {
                return await result.SafeWrapAsync(() => func(result.Result));
            }

            return (default(T2), result, reporter);
        }

        public static async Task<SafeWrapper<T>> OnSuccessAsync<T>(this SafeWrapper<T> wrapped, Func<T, Task<T>> func)
        {
            if (!AssertNotNull(wrapped, func)) return new SafeWrapper<T>(default(T), NullFunctionDelegateResult);

            SafeWrapper<T> result = wrapped;

            var reporter = TryGetReporter(result);

            if (result)
            {
                return await result.SafeWrapAsync(() => func(result.Result));
            }

            return (result, reporter);
        }

        public static async Task<SafeWrapperResult> OnSuccessAsync(this SafeWrapperResult wrapped, Func<Task> func)
        {
            if (!AssertNotNull(wrapped, func)) return NullFunctionDelegateResult;

            SafeWrapperResult result = wrapped;

            var reporter = TryGetReporter(result);

            if (result)
            {
                return await result.SafeWrapAsync(() => func());
            }

            return (result, reporter);
        }

        #endregion

        #region OnFailure

        public static SafeWrapper<T> OnFailure<T>(this SafeWrapper<T> wrapped, Action<SafeWrapper<T>> func)
        {
            if (!AssertNotNull(wrapped, func)) return new SafeWrapper<T>(default(T), NullFunctionDelegateResult);

            SafeWrapper<T> result = wrapped;

            var reporter = TryGetReporter(result);

            if (!result)
            {
                result.SafeWrap(() => func(wrapped));
            }

            return (result, reporter);
        }

        public static SafeWrapper<T2> OnFailure<T1, T2>(this SafeWrapper<T1> wrapped, Func<SafeWrapper<T1>, T2> func)
        {
            if (!AssertNotNull(wrapped, func)) return new SafeWrapper<T2>(default(T2), NullFunctionDelegateResult);

            SafeWrapper<T1> result = wrapped;

            var reporter = TryGetReporter(result);

            if (!result)
            {
                return result.SafeWrap(() => func(wrapped));
            }

            return (default(T2), result, reporter);
        }

        public static SafeWrapper<T> OnFailure<T>(this SafeWrapper<T> wrapped, Func<SafeWrapper<T>, T> func)
        {
            if (!AssertNotNull(wrapped, func)) return new SafeWrapper<T>(default(T), NullFunctionDelegateResult);

            SafeWrapper<T> result = wrapped;

            var reporter = TryGetReporter(result);

            if (!result)
            {
                return result.SafeWrap(() => func(wrapped));
            }

            return (result, reporter);
        }

        public static SafeWrapperResult OnFailure(this SafeWrapperResult wrapped, Action<SafeWrapperResult> func)
        {
            if (!AssertNotNull(wrapped, func)) return NullFunctionDelegateResult;

            SafeWrapperResult result = wrapped;

            var reporter = TryGetReporter(result);

            if (!result)
            {
                return result.SafeWrap(() => func(wrapped));
            }

            return (result, reporter);
        }

        public static SafeWrapperResult OnFailure(this SafeWrapperResult wrapped, Func<SafeWrapperResult, SafeWrapperResult> func)
        {
            if (!AssertNotNull(wrapped, func)) return NullFunctionDelegateResult;

            SafeWrapperResult result = wrapped;

            var reporter = TryGetReporter(result);

            if (!result)
            {
                return result.SafeWrap(() => func(wrapped));
            }

            return (result, reporter);
        }

        #endregion

        #region OnFailureAsync

        public static async Task<SafeWrapper<T>> OnFailureAsync<T>(this Task<SafeWrapper<T>> wrapped, Action<SafeWrapper<T>> func)
        {
            if (!AssertNotNull(wrapped, func)) return new SafeWrapper<T>(default(T), NullFunctionDelegateResult);

            SafeWrapper<T> result = await wrapped;

            var reporter = TryGetReporter(result);

            if (!result)
            {
                result.SafeWrap(() => func(result));
            }

            return (result, reporter);
        }

        public static async Task<SafeWrapper<T2>> OnFailureAsync<T1, T2>(this Task<SafeWrapper<T1>> wrapped, Func<SafeWrapper<T1>, Task<T2>> func)
        {
            if (!AssertNotNull(wrapped, func)) return new SafeWrapper<T2>(default(T2), NullFunctionDelegateResult);

            SafeWrapper<T1> result = await wrapped;

            var reporter = TryGetReporter(result);

            if (!result)
            {
                return await result.SafeWrapAsync(() => func(result));
            }

            return (default(T2), result, reporter);
        }

        public static async Task<SafeWrapper<T2>> OnFailureAsync<T1, T2>(this SafeWrapper<T1> wrapped, Func<SafeWrapper<T1>, Task<T2>> func)
        {
            if (!AssertNotNull(wrapped, func)) return new SafeWrapper<T2>(default(T2), NullFunctionDelegateResult);

            SafeWrapper<T1> result = wrapped;

            var reporter = TryGetReporter(result);

            if (!result)
            {
                return await result.SafeWrapAsync(() => func(result));
            }

            return (default(T2), result, reporter);
        }

        public static async Task<SafeWrapper<T>> OnFailureAsync<T>(this Task<SafeWrapper<T>> wrapped, Func<SafeWrapper<T>, Task<T>> func)
        {
            if (!AssertNotNull(wrapped, func)) return new SafeWrapper<T>(default(T), NullFunctionDelegateResult);

            SafeWrapper<T> result = await wrapped;

            var reporter = TryGetReporter(result);

            if (!result)
            {
                return await result.SafeWrapAsync(() => func(result));
            }

            return (result, reporter);
        }

        public static async Task<SafeWrapper<T>> OnFailureAsync<T>(this SafeWrapper<T> wrapped, Func<SafeWrapper<T>, Task<T>> func)
        {
            if (!AssertNotNull(wrapped, func)) return new SafeWrapper<T>(default(T), NullFunctionDelegateResult);

            SafeWrapper<T> result = wrapped;

            var reporter = TryGetReporter(result);

            if (!result)
            {
                return await result.SafeWrapAsync(() => func(result));
            }

            return (result, reporter);
        }

        public static async Task<SafeWrapperResult> OnFailureAsync(this Task<SafeWrapperResult> wrapped, Action<SafeWrapperResult> func)
        {
            if (!AssertNotNull(wrapped, func)) return NullFunctionDelegateResult;

            SafeWrapperResult result = await wrapped;

            var reporter = TryGetReporter(result);

            if (!result)
            {
                return result.SafeWrap(() => func(result));
            }

            return (result, reporter);
        }

        public static async Task<SafeWrapperResult> OnFailureAsync(this Task<SafeWrapperResult> wrapped, Func<SafeWrapperResult, Task<SafeWrapperResult>> func)
        {
            if (!AssertNotNull(wrapped, func)) return NullFunctionDelegateResult;

            SafeWrapperResult result = await wrapped;

            var reporter = TryGetReporter(result);

            if (!result)
            {
                return await result.SafeWrapAsync(() => func(result));
            }

            return (result, reporter);
        }

        public static async Task<SafeWrapperResult> OnFailureAsync(this SafeWrapperResult wrapped, Func<SafeWrapperResult, Task<SafeWrapperResult>> func)
        {
            if (!AssertNotNull(wrapped, func)) return NullFunctionDelegateResult;

            SafeWrapperResult result = wrapped;

            var reporter = TryGetReporter(result);

            if (!result)
            {
                return await result.SafeWrapAsync(() => func(result));
            }

            return (result, reporter);
        }

        #endregion

        #region LogOnFail

        public static SafeWrapperResult LogOnFail(this SafeWrapperResult wrapped, Logger logger)
        {
            return wrapped.OnFailure(res => logger?.Info(res.Exception, res.Message));
        }

        public static async Task<SafeWrapperResult> LogOnFailAsync(this Task<SafeWrapperResult> wrapped, Logger logger)
        {
            return await wrapped.OnFailureAsync((res) => logger?.Info(res.Exception, res.Message));
        }

        public static SafeWrapper<T> LogOnFail<T>(this SafeWrapper<T> wrapped, Logger logger)
        {
            return wrapped.OnFailure(res => logger?.Info(res.Exception, res.Message));
        }

        public static async Task<SafeWrapper<T>> LogOnFailAsync<T>(this Task<SafeWrapper<T>> wrapped, Logger logger)
        {
            return await wrapped.OnFailureAsync((res) => logger?.Info(res.Exception, res.Message));
        }

        #endregion

        #region SetReporter

        public static SafeWrapperResult SetReporter(Type reporter)
        {
            return SetReporter((ISafeWrapperExceptionReporter)Activator.CreateInstance(reporter));
        }

        public static SafeWrapperResult SetReporter(ISafeWrapperExceptionReporter reporter)
        {
            if (!AssertNotNull(reporter)) return NullFunctionDelegateResult;

            return (SafeWrapperResult.SUCCESS, reporter);
        }

        public static SafeWrapper<T> SetReporter<T>(Type reporter)
        {
            return SetReporter<T>((ISafeWrapperExceptionReporter)Activator.CreateInstance(reporter));
        }

        public static SafeWrapper<T> SetReporter<T>(ISafeWrapperExceptionReporter reporter)
        {
            if (!AssertNotNull(reporter)) return new SafeWrapper<T>(default(T), NullFunctionDelegateResult);

            return (default(T), SafeWrapperResult.SUCCESS, reporter);
        }

        public static SafeWrapperResult SetReporter(this SafeWrapperResult wrapped, Type reporter)
        {
            return SetReporter(wrapped, (ISafeWrapperExceptionReporter)Activator.CreateInstance(reporter));
        }

        public static SafeWrapperResult SetReporter(this SafeWrapperResult wrapped, ISafeWrapperExceptionReporter reporter)
        {
            if (!AssertNotNull(wrapped, reporter)) return NullFunctionDelegateResult;

            return (reporter.GetStatusResult(wrapped.Exception), reporter);
        }

        public static async Task<SafeWrapperResult> SetReporter(this Task<SafeWrapperResult> wrapped, Type reporter)
        {
            return await SetReporter(wrapped, (ISafeWrapperExceptionReporter)Activator.CreateInstance(reporter));
        }

        public static async Task<SafeWrapperResult> SetReporter(this Task<SafeWrapperResult> wrapped, ISafeWrapperExceptionReporter reporter)
        {
            if (!AssertNotNull(wrapped, reporter)) return NullFunctionDelegateResult;

            return (reporter.GetStatusResult((await wrapped).Exception), reporter);
        }

        public static SafeWrapper<T> SetReporter<T>(this SafeWrapper<T> wrapped, Type reporter)
        {
            return SetReporter(wrapped, (ISafeWrapperExceptionReporter)Activator.CreateInstance(reporter));
        }

        public static SafeWrapper<T> SetReporter<T>(this SafeWrapper<T> wrapped, ISafeWrapperExceptionReporter reporter)
        {
            if (!AssertNotNull(wrapped, reporter)) return new SafeWrapper<T>(default(T), NullFunctionDelegateResult);

            return (wrapped.Result, reporter.GetStatusResult(wrapped.Exception, typeof(T)), reporter);
        }

        public static async Task<SafeWrapper<T>> SetReporter<T>(this Task<SafeWrapper<T>> wrapped, Type reporter)
        {
            return await SetReporter(wrapped, (ISafeWrapperExceptionReporter)Activator.CreateInstance(reporter));
        }

        public static async Task<SafeWrapper<T>> SetReporter<T>(this Task<SafeWrapper<T>> wrapped, ISafeWrapperExceptionReporter reporter)
        {
            if (!AssertNotNull(wrapped, reporter)) return new SafeWrapper<T>(default(T), NullFunctionDelegateResult);

            var result = await wrapped;

            return (result.Result, reporter.GetStatusResult(result.Exception, typeof(T)), reporter);
        }

        #endregion

        private static bool AssertNotNull(params object[] objectsToCheck)
        {
            return !objectsToCheck.Any((item) => item == null);
        }

        private static ISafeWrapperExceptionReporter TryGetReporter(SafeWrapperResult wrapped)
        {
            return wrapped?.Reporter ?? new DefaultSafeWrapperExceptionReporter();
        }
    }
}
