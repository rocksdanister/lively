using System;

namespace Lively.Common.Exceptions;

public class WorkerWException : Exception
{
    public WorkerWException()
    {
    }

    public WorkerWException(string message)
        : base(message)
    {
    }

    public WorkerWException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
