using Godot;
using System.Linq;

/// <summary>
/// Drinks when the Bloomy's collection area overlaps Water.
/// Invokes Drink() on the water source.
/// </summary>
public partial class DrinkState : ConsumeResourceState<Water>
{
    protected override string TargetGroup => nameof(Water);
    protected override void PerformConsume(Water nearbyResource, BehaviourComponent c)
    {
        nearbyResource.Drink();
        c.GetBodyPart<BioComponent>()?.Drink();
    }

    protected override bool IsFull(BioComponent bio)
    {
        return bio.ThirstLevel <= 0.1f;
    }
}
