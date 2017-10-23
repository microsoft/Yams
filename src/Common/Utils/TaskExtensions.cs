using System;
using System.Threading;
using System.Threading.Tasks;

namespace Etg.Yams.Utils
{
    public static class TaskExtensions
    {
        public static async Task Timeout(this Task task, TimeSpan timeout, string timeoutMessage="")
        {
            var timeoutCancellationTokenSource = new CancellationTokenSource();

            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
            if (completedTask == task)
            {
                timeoutCancellationTokenSource.Cancel();
                await task;
            }
            else
            {
                throw new TimeoutException(timeoutMessage);
            }
        }

        public static async Task<TResult> Timeout<TResult>(this Task<TResult> task, TimeSpan timeout, string timeoutMessage = "")
        {

            var timeoutCancellationTokenSource = new CancellationTokenSource();

            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
            if (completedTask == task)
            {
                timeoutCancellationTokenSource.Cancel();
                return await task;
            }
            throw new TimeoutException(timeoutMessage);
        }
    }
}