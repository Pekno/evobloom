using System.Collections.Generic;

/// <summary>
/// Base class for all “feelings.”
/// Each Compute(…) call updates Weight based on BioComponent and memories.
/// </summary>
public abstract class Feeling
{
    public FeelingType Type { get; }
    public float Weight { get; protected set; }

    protected Feeling(FeelingType type)
    {
        Type = type;
        Weight = 0f;
    }

    /// <summary>
    /// Compute this feeling’s Weight.  
    /// <paramref name="bio"/> gives current hunger/thirst,  
    /// <paramref name="memories"/> is the Brain’s long-term memory.  
    /// </summary>
    public abstract void Compute(BioComponent bio, IEnumerable<MemoryVisionResult> memories, float currentTime, float decayRate);
}