using System;

namespace Lively.Common.Services
{
    public interface IDispatcherService
    {
        bool TryEnqueue(Action action);
    }
}
