using System.Collections.Generic;

public class Hunger : Feeling
{
    public Hunger() : base(FeelingType.Hunger) { }

    public override void Compute(BioComponent bio, IEnumerable<MemoryVisionResult> memories, float currentTime, float decayRate)
    {
        // Simply proportional to how hungry we are
        Weight = bio?.HungerLevel ?? 0f;
    }
}