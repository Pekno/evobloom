using Godot;
using System.Linq;

/// <summary>
/// Gathers berries when the Bloomy's collection area overlaps a BerryBush.
/// Invokes Harvest() on the bush and feeds the Bloomy immediately.
/// </summary>

public partial class EatState : ConsumeResourceState<Fruit>
{
    protected override string TargetGroup => nameof(Fruit);
    protected override void PerformConsume(Fruit nearbyResource, BehaviourComponent c)
    {
        var hungerLevel = nearbyResource.Eat();
        c.GetBodyPart<BioComponent>()?.Eat(hungerLevel);
    }

    protected override bool IsFull(BioComponent bio)
    {
        return bio.HungerLevel <= 0.1f;
    }
}