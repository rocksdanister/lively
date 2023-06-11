namespace Lively.ML.DepthEstimate
{
    public interface IDepthEstimate
    {
        ModelOutput Run(string imagePath);
    }
}