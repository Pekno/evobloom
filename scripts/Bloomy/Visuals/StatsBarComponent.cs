using Godot;
using System;

/// <summary>
/// Draws hunger and thirst bars under the Bloomy using BioComponent values.
/// Bars represent fullness: 1 = full, 0 = empty. Also displays threshold markers.
/// </summary>
public partial class StatsBarComponent : BloomyComponent
{
    [Export] public Vector2 Offset = new Vector2(0, 40);
    [Export] public Vector2 Size = new Vector2(40, 6);
    [Export] public float Spacing = 2f;
    [Export] public Color BackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
    [Export] public Color HungerColor = Colors.Lime;
    [Export] public Color ThirstColor = Colors.Cyan;
    [Export] public Color ThresholdColor = Colors.White;

    private BioComponent _bio;

    public override void _Ready()
    {
        _bio = GetParent<BioComponent>();
        if (_bio == null)
            GD.PrintErr("StatsBarComponent: must be a child of BioComponent!");
    }

    public override void ProcessComponent(float delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_bio == null)
            return;

        // Calculate fullness values (1 = full)
        float hungerFullness = 1f - _bio.HungerLevel;
        float thirstQuench = 1f - _bio.ThirstLevel;

        // Calculate threshold positions
        float hungerThresholdFull = 1f - _bio.HungerThreshold;
        float thirstThresholdQuench = 1f - _bio.ThirstThreshold;

        // Positions
        Vector2 hungerBgPos = Offset - new Vector2(Size[0] / 2, 0);
        Vector2 thirstBgPos = Offset + new Vector2(0, Size[1] + Spacing) - new Vector2(Size[0] / 2, 0);

        // Draw hunger background and fill
        DrawRect(new Rect2(hungerBgPos, Size), BackgroundColor);
        DrawRect(new Rect2(hungerBgPos, new Vector2(Size[0] * hungerFullness, Size[1])), HungerColor);
        // Draw hunger threshold marker
        float hungerMarkerX = hungerBgPos[0] + Size[0] * hungerThresholdFull;
        DrawLine(
            new Vector2(hungerMarkerX, hungerBgPos[1]),
            new Vector2(hungerMarkerX, hungerBgPos[1] + Size[1]),
            ThresholdColor,
            2f
        );

        // Draw thirst background and fill
        DrawRect(new Rect2(thirstBgPos, Size), BackgroundColor);
        DrawRect(new Rect2(thirstBgPos, new Vector2(Size[0] * thirstQuench, Size[1])), ThirstColor);
        // Draw thirst threshold marker
        float thirstMarkerX = thirstBgPos[0] + Size[0] * thirstThresholdQuench;
        DrawLine(
            new Vector2(thirstMarkerX, thirstBgPos[1]),
            new Vector2(thirstMarkerX, thirstBgPos[1] + Size[1]),
            ThresholdColor,
            2f
        );
    }
}
