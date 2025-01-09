using System;

namespace Lively.Models.Exceptions;

public class ScreenNotFoundException : Exception
{
    public ScreenNotFoundException()
    {
    }

    public ScreenNotFoundException(string message)
        : base(message)
    {
    }

    public ScreenNotFoundException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
