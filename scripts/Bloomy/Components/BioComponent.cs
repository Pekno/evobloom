using Godot;

/// <summary>
/// Tracks physiological needs (Hunger and Thirst) for a Bloomy.
/// Emits signals when levels cross urgency thresholds and when eating/drinking.
/// </summary>
public partial class BioComponent : BloomyComponent
{
    /// <summary>Signal emitted when hunger reaches the configured threshold.</summary>
    [Signal] public delegate void HungerThresholdReachedEventHandler();
    /// <summary>Signal emitted when thirst reaches the configured threshold.</summary>
    [Signal] public delegate void ThirstThresholdReachedEventHandler();
    /// <summary>Signal emitted when the Bloomy eats.</summary>
    [Signal] public delegate void AteEventHandler();
    /// <summary>Signal emitted when the Bloomy drinks.</summary>
    [Signal] public delegate void DrankEventHandler();

    /// <summary>Initial hunger level (0 = full, 1 = starving).</summary>
    [Export] public float InitialHungerLevel = 0f;
    /// <summary>Initial thirst level (0 = quenched, 1 = parched).</summary>
    [Export] public float InitialThirstLevel = 0f;
    /// <summary>Hunger increase rate per second.</summary>
    [Export] public float BaseHungerIncreaseRate = 0.005f;
    /// <summary>Thirst increase rate per second.</summary>
    [Export] public float BaseThirstIncreaseRate = 0.01f;
    /// <summary>Threshold at which hunger is considered urgent (0-1).</summary>
    [Export] public float HungerThreshold = 0.8f;
    /// <summary>Threshold at which thirst is considered urgent (0-1).</summary>
    [Export] public float ThirstThreshold = 0.65f;

    private float _hungerLevel;
    private float _thirstLevel;
    private bool _hungerAlerted = false;
    private bool _thirstAlerted = false;

    private float _effectiveHungerIncreaseRate;
    private float _effectiveThirstIncreaseRate;
    private DNAComponent _dna;

    /// <summary>Current hunger level (0 = full, 1 = starving).</summary>
    public float HungerLevel => _hungerLevel;
    /// <summary>Current thirst level (0 = quenched, 1 = parched).</summary>
    public float ThirstLevel => _thirstLevel;

    public override void _Ready()
    {
        _dna = GetBodyPart<DNAComponent>();
        if (_dna == null)
        {
            GD.PrintErr($"*{_bloomy?.Surname}* BioComponent: DNAComponent not found! Using base rates for hunger/thirst.");
            _effectiveHungerIncreaseRate = BaseHungerIncreaseRate;
            _effectiveThirstIncreaseRate = BaseThirstIncreaseRate;
        }
        else
        {
            _effectiveHungerIncreaseRate = BaseHungerIncreaseRate * _dna.GetTraitValue(DNATraitType.HungerRateMultiplier);
            _effectiveThirstIncreaseRate = BaseThirstIncreaseRate * _dna.GetTraitValue(DNATraitType.ThirstRateMultiplier);
        }
        // Initialize levels
        _hungerLevel = Mathf.Clamp(InitialHungerLevel, 0f, 1f);
        _thirstLevel = Mathf.Clamp(InitialThirstLevel, 0f, 1f);
    }

    /// <summary>
    /// Called each frame to update hunger and thirst over time
    /// and emit threshold signals when crossed.
    /// </summary>
    public override void ProcessComponent(float delta)
    {
        // Increase and clamp levels
        _hungerLevel = Mathf.Clamp(_hungerLevel + _effectiveHungerIncreaseRate * delta, 0f, 1f);
        _thirstLevel = Mathf.Clamp(_thirstLevel + _effectiveThirstIncreaseRate * delta, 0f, 1f);

        // Check thresholds
        if (!_hungerAlerted && _hungerLevel >= HungerThreshold)
        {
            _hungerAlerted = true;
            EmitSignal(nameof(HungerThresholdReached));
        }
        if (!_thirstAlerted && _thirstLevel >= ThirstThreshold)
        {
            _thirstAlerted = true;
            EmitSignal(nameof(ThirstThresholdReached));
        }
    }

    /// <summary>
    /// Reduces hunger level and emits Ate signal.
    /// </summary>
    public void Eat(float amount = 1f)
    {
        _hungerLevel = Mathf.Clamp(_hungerLevel - amount, 0f, 1f);
        if (_hungerAlerted && _hungerLevel < HungerThreshold)
            _hungerAlerted = false;
        EmitSignal(nameof(Ate));
    }

    /// <summary>
    /// Reduces thirst level and emits Drank signal.
    /// </summary>
    public void Drink(float amount = 1f)
    {
        _thirstLevel = Mathf.Clamp(_thirstLevel - amount, 0f, 1f);
        if (_thirstAlerted && _thirstLevel < ThirstThreshold)
            _thirstAlerted = false;
        EmitSignal(nameof(Drank));
    }
}
