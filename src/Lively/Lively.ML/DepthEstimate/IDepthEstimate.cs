namespace Lively.ML.DepthEstimate
{
    public interface IDepthEstimate : IDisposable
    {
        ModelOutput Run(string imagePath);
    }
}