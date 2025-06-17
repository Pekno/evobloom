using System.Collections.Generic;

public class Boredom : Feeling
{
    private const float BaseBoredomWeight = 0.2f;

    public Boredom() : base(FeelingType.Boredom) { }

    public override void Compute(BioComponent bio, IEnumerable<MemoryVisionResult> memories, float currentTime, float decayRate)
    {
        // Simple constant drive to wander if nothing else is urgent
        Weight = BaseBoredomWeight;
    }
}
