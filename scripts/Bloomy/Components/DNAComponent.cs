using Godot;
using System.Collections.Generic;
using System.Linq; // For ToDictionary if needed, or just manual population

public partial class DNAComponent : BloomyComponent
{
    // Stores the genetic traits of this Bloomy. Values are typically multipliers (1.0 = average).
    private readonly Dictionary<DNATraitType, float> _traits = new();

    private RandomNumberGenerator _rng = new RandomNumberGenerator();

    public override void _Ready()
    {
        InitializeDefaultDNA(); // Call a method to populate for clarity

        if (Debug && _bloomy != null)
        {
            PrintDNA();
        }
    }

    private void InitializeDefaultDNA()
    {
        // For now, until reproduction is implemented, we can randomize these slightly
        // to give some initial variation to spawned Bloomies.
        // In a full reproduction system, these values would be inherited.
        _rng.Randomize();

        // Define default base values and randomization range if desired
        _traits[DNATraitType.HungerRateMultiplier] = _rng.RandfRange(0.8f, 1.2f);
        _traits[DNATraitType.ThirstRateMultiplier] = _rng.RandfRange(0.8f, 1.2f);
        _traits[DNATraitType.MemoryDecayRateMultiplier] = _rng.RandfRange(0.8f, 1.2f);
        _traits[DNATraitType.SpeedMultiplier] = _rng.RandfRange(0.8f, 1.2f);
        _traits[DNATraitType.MaturationRateMultiplier] = _rng.RandfRange(0.8f, 1.2f);
        _traits[DNATraitType.SensoryRangeMultiplier] = _rng.RandfRange(0.8f, 1.2f);

        // Ensure all defined DNATraitTypes have a default if not randomized above
        foreach (DNATraitType traitType in System.Enum.GetValues(typeof(DNATraitType)))
        {
            if (!_traits.ContainsKey(traitType))
            {
                _traits[traitType] = 1.0f; // Default to 1.0 if not specifically set
            }
        }
    }

    public override void ProcessComponent(float delta)
    {
        // No continuous processing needed for static DNA traits.
    }

    /// <summary>
    /// Gets the value of a specific genetic trait.
    /// </summary>
    /// <param name="traitType">The type of trait to retrieve.</param>
    /// <param name="defaultValue">The value to return if the trait is not found (should ideally not happen if initialized correctly).</param>
    /// <returns>The float value of the trait.</returns>
    public float GetTraitValue(DNATraitType traitType, float defaultValue = 1.0f)
    {
        return _traits.TryGetValue(traitType, out float value) ? value : defaultValue;
    }

    /// <summary>
    /// Gets all traits as a read-only dictionary. Useful for UI or other systems.
    /// </summary>
    public IReadOnlyDictionary<DNATraitType, float> GetAllTraits()
    {
        return _traits;
    }

    /// <summary>
    /// Initializes or overrides the DNA with a given set of traits.
    /// Useful for reproduction or specific scenario setups.
    /// </summary>
    /// <param name="newTraits">A dictionary of traits to set.</param>
    public void InitializeDNA(Dictionary<DNATraitType, float> newTraits)
    {
        _traits.Clear(); // Clear existing defaults if any
        foreach (var traitPair in newTraits)
        {
            _traits[traitPair.Key] = traitPair.Value;
        }

        // Ensure all defined DNATraitTypes have a value, falling back to default if not in newTraits
        foreach (DNATraitType traitType in System.Enum.GetValues(typeof(DNATraitType)))
        {
            if (!_traits.ContainsKey(traitType))
            {
                _traits[traitType] = 1.0f; // Default if missing from the provided dictionary
            }
        }

        if (Debug && _bloomy != null)
        {
            GD.Print($"*{_bloomy.Surname}* DNA Re-Initialized:");
            PrintDNA();
        }
    }

    private void PrintDNA()
    {
        if (_bloomy == null) return;
        var traitStrings = _traits.Select(kvp => $"{kvp.Key}: {kvp.Value:0.00}");
        GD.Print($"*{_bloomy.Surname}* DNA: {string.Join(", ", traitStrings)}");
    }
}