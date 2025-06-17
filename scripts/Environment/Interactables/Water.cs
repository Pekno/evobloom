using Godot;

/// <summary>
/// Represents a berry bush that can be harvested by Bloomies.
/// When BerryCount reaches zero, the bush is removed.
/// </summary>
public partial class Water : CanBeSeenNode2D
{
    /// <summary>
    /// Signal emitted whenever some water is drunk.
    /// Includes the water instance.
    /// </summary>
    [Signal] public delegate void WaterDrunkEventHandler(Water water);

    /// <summary>
    /// Drink the specified amount of water.
    /// </summary>
    public void Drink()
    {
        // Emit the water body reference so listeners know where to spawn effects
        EmitSignal(nameof(WaterDrunk), this);
    }
}
