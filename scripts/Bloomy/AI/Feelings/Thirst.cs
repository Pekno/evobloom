using System.Collections.Generic;

public class Thirst : Feeling
{
    public Thirst() : base(FeelingType.Thirst) { }

    public override void Compute(BioComponent bio, IEnumerable<MemoryVisionResult> memories, float currentTime, float decayRate)
    {
        // Proportional to how thirsty we are
        Weight = bio?.ThirstLevel ?? 0f;
    }
}
