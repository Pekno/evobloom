using Godot;
using System.Collections.Generic;

public partial class SightComponent : BloomyComponent
{
    [Export] public Area2D SensorArea;
    private float _baseSensorRadius = -1f;
    private BrainComponent _brain;

    private DNAComponent _dna;

    public List<Node> DetectedObjects { get; private set; } = new List<Node>();

    private float CurrentSightRadius
    {
        get
        {
            float multiplier = _dna.GetTraitValue(DNATraitType.SensoryRangeMultiplier, 1.0f);
            float newRadius = _baseSensorRadius * multiplier;
            return newRadius;
        }
    }

    // 🌟 A generic detection signal:
    [Signal]
    public delegate void ThingsDetectedEventHandler(VisionResult[] detectedObjects);

    public override void _Ready()
    {
        _brain = GetBodyPart<BrainComponent>();
        _dna = GetBodyPart<DNAComponent>();
        if (SensorArea != null)
        {
            SensorArea.Connect("body_entered", new Callable(this, nameof(OnBodyEntered)));
            SensorArea.Connect("body_exited", new Callable(this, nameof(OnBodyExited)));

            // Apply DNA modifications for sight area
            var sensorCollisionShape = SensorArea.GetChild<CollisionShape2D>(0);
            if (sensorCollisionShape.Shape is CircleShape2D circleShape && circleShape != null)
            {
                _baseSensorRadius = circleShape.Radius;
                circleShape.Radius = CurrentSightRadius;
            }
        }
        else
        {
            GD.PrintErr("SightComponent: SensorPath not set!");
        }
    }

    public override void ProcessComponent(float delta)
    {
        QueueRedraw();
    }

    private void OnBodyEntered(Node body)
    {
        if (!DetectedObjects.Contains(body))
            DetectedObjects.Add(body);
    }

    private void OnBodyExited(Node body)
    {
        if (DetectedObjects.Contains(body))
            DetectedObjects.Remove(body);
    }

    public List<VisionResult> CheckAround()
    {
        var results = new List<VisionResult>();

        foreach (var obj in DetectedObjects)
        {
            if (obj == null || !obj.IsInsideTree())
                continue;

            var canBeSeenNode2D = obj.GetParent<CanBeSeenNode2D>();
            if (canBeSeenNode2D == null)
                continue;

            var type = VisionTypeProperties.GetVisionTypeFromGroups(obj);
            if (type == VisionType.Unknown)
                continue;

            var position = obj is Node2D node2D ? node2D.GlobalPosition : Vector2.Zero;
            var visionResult = new VisionResult(canBeSeenNode2D, position, type);

            results.Add(visionResult);
        }
        if (results.Count > 0)
            EmitSignal(nameof(ThingsDetected), results.ToArray());

        return results;
    }

    public override void _Draw()
    {
        if (!Debug) return;

        DrawCircle(ToLocal(GlobalPosition), CurrentSightRadius, new Color(0, 1, 1, 0.2f));
    }
}
