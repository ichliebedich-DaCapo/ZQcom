using System;
using System.Threading.Tasks;

namespace ZQcom.Helpers
{
    public static class TaskExtensions
    {
        public static async Task WithTimeout(this Task task, TimeSpan timeout)
        {
            var delay = Task.Delay(timeout);

            var completedTask = await Task.WhenAny(task, delay);

            if (completedTask == task)
            {
                await task; // 确保任务完成
            }
            else
            {
                throw new TimeoutException("The operation timed out.");
            }
        }
    }
}