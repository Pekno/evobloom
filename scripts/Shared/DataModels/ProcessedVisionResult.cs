using Godot;

public partial class ProcessedVisionResult : VisionResult
{
    /// <summary>
    /// The calculated "desirability" or "relevance" score of this vision at the moment of processing.
    /// Used for immediate decision-making (e.g., selecting a target).
    /// </summary>
    public float Weight { get; set; }

    /// <summary>
    /// The calculated initial strength this vision would have if committed to long-term memory.
    /// This is determined by factors like inherent importance of the vision type and current physiological needs.
    /// </summary>
    public float CalculatedInitialMemoryStrength { get; set; }

    public ProcessedVisionResult(VisionResult visionResult, float weight, float calculatedInitialMemoryStrength)
        : base(visionResult.Target, visionResult.Position, visionResult.Type)
    {
        Weight = weight;
        CalculatedInitialMemoryStrength = calculatedInitialMemoryStrength;
    }
}