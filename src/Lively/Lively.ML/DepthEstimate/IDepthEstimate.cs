namespace Lively.ML.DepthEstimate
{
    public interface IDepthEstimate : IDisposable
    {
        void LoadModel(string path);
        ModelOutput Run(string imagePath);
        string ModelPath { get; }
    }
}