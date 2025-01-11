using System;

namespace Lively.Common.Services
{
    public interface IAudioVisualizerService : IDisposable
    {
        event EventHandler<double[]> AudioDataAvailable;
        void Start();
        void Stop();
    }
}
