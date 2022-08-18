using System;
using System.Collections.Generic;
using System.Text;

namespace Lively.Common.Services
{
    public interface IAudioVisualiserService : IDisposable
    {
        event EventHandler<double[]> AudioDataAvailable;
    }
}
