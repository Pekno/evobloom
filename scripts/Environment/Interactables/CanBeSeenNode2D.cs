using Godot;

public abstract partial class CanBeSeenNode2D : Node2D, ICanConsidered
{
    public virtual bool IsConsidered()
    {
        return true;
    }
}