using System.Collections.Generic;
using System.Linq;
using Godot;

public class Heat : Feeling
{
    private const float BaseHeatWeight = 0.5f;

    public Heat() : base(FeelingType.Heat) { }

    public override void Compute(BioComponent bio, IEnumerable<MemoryVisionResult> memories, float currentTime, float decayRate)
    {
        // Example: if we’ve seen “Shelter” memories, the older/less trusted they are,
        // the stronger the drive to seek refuge.  
        var shelterMemories = memories
            .Where(m => m.Type == VisionType.Shelter)
            .ToList();

        if (!shelterMemories.Any())
        {
            // No shelter known → mild heat discomfort
            Weight = BaseHeatWeight * 0.5f;
        }
        else
        {
            float avgTrust = shelterMemories
                .Select(m => m.GetCurrentStrength(currentTime, decayRate))
                .Average();
            // The less we trust shelter memory, the higher our drive
            Weight = BaseHeatWeight * (1f - avgTrust);
        }
    }
}