using Godot;
using System.Linq;

/// <summary>
/// Gathers berries when the Bloomy's collection area overlaps a BerryBush.
/// Invokes Harvest() on the bush and feeds the Bloomy immediately.
/// </summary>

public partial class ShakeState : ConsumeResourceState<FruitTree>
{
    protected override string TargetGroup => nameof(FruitTree);
    protected override void PerformConsume(FruitTree nearbyResource, BehaviourComponent c)
    {
        nearbyResource.Shake();
    }

    protected override bool IsFull(BioComponent bio)
    {
        // Shaking itself doesn't make you full.
        // This method is used in ShouldTransition's !isFull check.
        // If we want to shake even if slightly hungry, but not starving:
        // return bio.HungerLevel <= 0.1f; // Still makes sense as a prerequisite
        // Or, if shaking is independent of current fullness (e.g., to stock up, which isn't current AI)
        // return false; // Always "not full" for the purpose of this state's transition.
        // For now, keeping it tied to hunger makes sense for the Bloomy's motivation.
        return bio.HungerLevel <= 0.2f; // This is probably still the correct motivation.
    }
}
