using System.Collections.Generic;
using System.Linq;
using Godot;

public class Fear : Feeling
{
    private const float BaseFearWeight = 1.0f;

    public Fear() : base(FeelingType.Fear) { }

    public override void Compute(BioComponent bio, IEnumerable<MemoryVisionResult> memories, float currentTime, float decayRate)
    {
        // Look for any threatening memories (Threat or Predator)
        // Weight = BaseFearWeight × max trust of any threat memory
        float maxTrust = memories
            .Where(m => m.Type == VisionType.Threat || m.Type == VisionType.Predator)
            .Select(m => m.GetCurrentStrength(currentTime, decayRate))
            .DefaultIfEmpty(0f)
            .Max();

        Weight = BaseFearWeight * maxTrust;
    }
}