using System;

namespace Lively
{
    public interface ISystray : IDisposable
    {
        void ShowBalloonNotification(int timeout, string title, string msg);
        void Visibility(bool visible);
    }
}