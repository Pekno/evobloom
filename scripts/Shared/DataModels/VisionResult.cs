using Godot;

public partial class VisionResult : GodotObject
{
    public CanBeSeenNode2D Target { get; set; }
    public Vector2 Position { get; set; }
    public VisionType Type { get; set; }

    public VisionResult(CanBeSeenNode2D target, Vector2 position, VisionType type)
    {
        Target = target;
        Position = position;
        Type = type;
    }

    public bool Equals(VisionResult other)
    {
        if (other == null) return false;
        return Target == other.Target && Position == other.Position && Type == other.Type;
    }

    public bool Equals(MemoryVisionResult other)
    {
        return Equals(other as VisionResult);
    }
}
