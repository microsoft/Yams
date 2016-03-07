// -------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// -------------------------------------------------------------------

using System;
using System.Threading.Tasks;

namespace Etg.Yams.TestUtils
{
    public class AsyncUtils
    {
        public static Task<TReturnType> AsyncTaskThatThrows<TReturnType>(Exception exception)
        {
            TaskCompletionSource<TReturnType> tcs = new TaskCompletionSource<TReturnType>();
            tcs.SetException(exception);
            return tcs.Task;
        }

        public static Task AsyncTaskThatThrows(Exception exception)
        {
            return AsyncTaskThatThrows<bool>(exception);
        }

        public static Task<TReturnType> AsyncTaskWithResult<TReturnType>(TReturnType result)
        {
            TaskCompletionSource<TReturnType> tcs = new TaskCompletionSource<TReturnType>();
            tcs.SetResult(result);
            return tcs.Task;
        }
    }
}