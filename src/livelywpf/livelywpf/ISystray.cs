using System;

namespace livelywpf
{
    public interface ISystray : IDisposable
    {
        void ShowBalloonNotification(int timeout, string title, string msg);
    }
}