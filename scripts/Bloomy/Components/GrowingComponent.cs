using Godot;
using System.Collections.Generic;

public partial class GrowingComponent : BloomyComponent
{
    [Signal]
    public delegate void LifeStageChangedEventHandler(LifeStage newStage, LifeStage oldStage);

    [ExportGroup("Aging Parameters")]
    [Export] public float BaseAgeUnitsPerSecond = 0.1f; // How many "age units" pass per real-time second. e.g., 1.0 means 1 age unit per sec.
    [Export] public float CurrentAge { get; private set; } = 0f;

    [ExportGroup("Life Stage Thresholds (Age Units)")]
    [Export] public float YounglingAgeThreshold = 20f;  // Becomes Youngling at this age
    [Export] public float AdolescentAgeThreshold = 50f; // Becomes Adolescent at this age
    [Export] public float AdultAgeThreshold = 100f;     // Becomes Adult at this age
    [Export] public float OldAgeThreshold = 200f;       // Becomes Old at this age
    private DNAComponent _dna;
    public LifeStage CurrentLifeStage { get; private set; } = LifeStage.Baby;

    private readonly Dictionary<LifeStage, StageModifiers> _stageModifiers = new();

    public override void _Ready()
    {
        _dna = GetBodyPart<DNAComponent>(); // << NEW
        if (_dna == null)
        {
            GD.PrintErr($"*{_bloomy?.Surname}* GrowingComponent: DNAComponent not found! Maturation rate will not use genetic multiplier.");
        }

        // Initialize default modifiers for each stage, now including VisualScale
        _stageModifiers[LifeStage.Baby] = new StageModifiers(
            memoryDecay: 1.5f, speed: 0.5f, visualScale: new Vector2(0.5f, 0.5f)
        );
        _stageModifiers[LifeStage.Youngling] = new StageModifiers(
            memoryDecay: 1.0f, speed: 0.8f, visualScale: new Vector2(0.7f, 0.7f)
        );
        _stageModifiers[LifeStage.Adolescent] = new StageModifiers(
            memoryDecay: 0.8f, speed: 1.1f, visualScale: new Vector2(0.9f, 0.9f)
        // Note: CanReproduce for Adolescent could be set here if desired, e.g., canReproduce: true
        );
        _stageModifiers[LifeStage.Adult] = new StageModifiers(
            memoryDecay: 1.0f, speed: 1.0f, canReproduce: true, visualScale: new Vector2(1.0f, 1.0f)
        );
        _stageModifiers[LifeStage.Old] = new StageModifiers(
            memoryDecay: 1.8f, speed: 0.7f, visualScale: new Vector2(1.0f, 1.0f) // Old keeps adult scale
        );

        UpdateLifeStage(true); // Initial check and set
    }

    public override void ProcessComponent(float delta)
    {
        float maturationRate = _dna?.GetTraitValue(DNATraitType.MaturationRateMultiplier) ?? 1.0f;
        CurrentAge += (BaseAgeUnitsPerSecond * maturationRate) * delta;
        UpdateLifeStage();
    }

    private void UpdateLifeStage(bool forceUpdate = false)
    {
        LifeStage newStage = CurrentLifeStage;

        if (CurrentAge >= OldAgeThreshold)
            newStage = LifeStage.Old;
        else if (CurrentAge >= AdultAgeThreshold)
            newStage = LifeStage.Adult;
        else if (CurrentAge >= AdolescentAgeThreshold)
            newStage = LifeStage.Adolescent;
        else if (CurrentAge >= YounglingAgeThreshold)
            newStage = LifeStage.Youngling;
        else
            newStage = LifeStage.Baby; // Default if below all thresholds

        if (newStage != CurrentLifeStage || forceUpdate)
        {
            LifeStage oldStage = CurrentLifeStage;
            CurrentLifeStage = newStage;
            EmitSignal(nameof(LifeStageChanged), (int)CurrentLifeStage, (int)oldStage);

            if (Debug && _bloomy != null) // Add bloomy null check for safety during init
                GD.Print($"*{_bloomy.Surname}* aged into: {CurrentLifeStage} (Age: {CurrentAge:0.0})");
        }
    }

    public float GetCurrentMemoryDecayFactor()
    {
        return _stageModifiers.TryGetValue(CurrentLifeStage, out var modifiers) ? modifiers.MemoryDecayFactor : 1.0f;
    }

    public float GetCurrentSpeedFactor()
    {
        return _stageModifiers.TryGetValue(CurrentLifeStage, out var modifiers) ? modifiers.SpeedFactor : 1.0f;
    }

    public bool CanReproduce()
    {
        return _stageModifiers.TryGetValue(CurrentLifeStage, out var modifiers) ? modifiers.CanReproduce : false;
    }

    public Vector2 GetCurrentVisualScale()
    {
        return _stageModifiers.TryGetValue(CurrentLifeStage, out var modifiers) ? modifiers.VisualScale : Vector2.One;
    }

    // Optional: Public method to get current age string for UI or debugging
    public string GetAgeDisplay() => $"{CurrentAge:0.0} units ({CurrentLifeStage})";
}