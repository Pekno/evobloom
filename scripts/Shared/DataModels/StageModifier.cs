using Godot;

public partial class StageModifiers : GodotObject
{
    public float MemoryDecayFactor { get; set; } = 1.0f;
    public float SpeedFactor { get; set; } = 1.0f;
    public bool CanReproduce { get; set; } = false;
    // public float EnergyConsumptionFactor { get; set; } = 1.0f; // Example for future
    public Vector2 VisualScale { get; set; } = Vector2.One;

    public StageModifiers(float memoryDecay = 1.0f, float speed = 1.0f, bool canReproduce = false, Vector2? visualScale = null)
    {
        MemoryDecayFactor = memoryDecay;
        SpeedFactor = speed;
        CanReproduce = canReproduce;
        VisualScale = visualScale ?? Vector2.One;
    }
}