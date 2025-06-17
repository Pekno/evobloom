using Godot;

public partial class SidebarMenuFeeling : Control
{
    [Export] public Label FeelingNameLabelNode { get; private set; }
    [Export] public HSlider FeelingSliderNode { get; private set; }
    [Export] public Label FeelingValueLabelNode { get; private set; }

    private FeelingType _feelingType;

    public override void _Ready()
    {
        // Ensure nodes are assigned in the editor via [Export]
        if (FeelingNameLabelNode == null || FeelingSliderNode == null || FeelingValueLabelNode == null)
        {
            GD.PrintErr("SidebarMenuFeeling: One or more required nodes are not assigned in the editor!");
            QueueFree(); // Remove itself if not set up correctly
            return;
        }
    }

    public void Initialize(FeelingType type, float initialWeight, float maxWeight = 1.0f)
    {
        _feelingType = type;
        FeelingNameLabelNode.Text = type.ToString();
        FeelingSliderNode.MaxValue = maxWeight;
        UpdateValue(initialWeight);
    }

    public void UpdateValue(float weight)
    {
        if (FeelingSliderNode == null || FeelingValueLabelNode == null) return;

        FeelingSliderNode.Value = weight;
        FeelingValueLabelNode.Text = $"{weight:0.00}";
    }

    public void Highlight(bool isHighlighted)
    {
        // Example: Change background color or font color of the name label
        if (isHighlighted)
        {
            FeelingNameLabelNode.Modulate = Colors.Yellow; // Or add a stylebox
        }
        else
        {
            FeelingNameLabelNode.Modulate = Colors.White; // Reset to default
        }
    }

    public FeelingType GetFeelingType()
    {
        return _feelingType;
    }
}