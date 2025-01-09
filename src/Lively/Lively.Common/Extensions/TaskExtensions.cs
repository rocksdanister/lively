using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.Common.Extensions
{
    public static class TaskExtensions
    {
        //Ref: https://www.youtube.com/watch?v=O1Tx-k4Vao0
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Bug", "S3168:\"async\" methods should not return \"void\"", Justification = "Error is handled as event")]
        public async static void Await(this Task task, Action completedCallBack, Action<Exception> errorCallBack)
        {
            try
            {
                await task;
                completedCallBack?.Invoke();
            }
            catch (Exception ex)
            {
                errorCallBack?.Invoke(ex);
            }
        }

        public static void TaskWaitCancel(this CancellationTokenSource cts)
        {
            if (cts == null)
                return;

            cts.Cancel();
            cts.Dispose();
        }

        public static bool IsTaskWaitCompleted(this Task task)
        {
            if (task != null)
            {
                if ((task.IsCompleted == false
                || task.Status == TaskStatus.Running
                || task.Status == TaskStatus.WaitingToRun
                || task.Status == TaskStatus.WaitingForActivation))
                {
                    return false;
                }
                return true;
            }
            return true;
        }
    }
}
