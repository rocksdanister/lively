namespace Lively.ML.DepthEstimate
{
    public class ModelOutput
    {
        public ModelOutput(float[] depth)
        {
            Depth = depth;
        }

        public float[] Depth { get; set; }
    }
}
