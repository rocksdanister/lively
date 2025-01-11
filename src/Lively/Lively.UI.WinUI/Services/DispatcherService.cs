using Lively.Common.Services;
using Microsoft.UI.Dispatching;
using System;

namespace Lively.UI.WinUI.Services
{
    public class DispatcherService : IDispatcherService
    {
        private readonly DispatcherQueue dispatcherQueue;

        public DispatcherService()
        {
            // MainWindow dispatcher may not be ready yet, creating our own instead.
            dispatcherQueue = DispatcherQueue.GetForCurrentThread() ?? DispatcherQueueController.CreateOnCurrentThread().DispatcherQueue;
        }

        public bool TryEnqueue(Action action)
        {
            return dispatcherQueue.TryEnqueue(() => action());
        }
    }
}
