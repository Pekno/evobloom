using Godot;

public partial class MemoryVisionResult : VisionResult
{
    /// <summary>
    /// The game time (in seconds) when this memory was last seen or reinforced.
    /// </summary>
    public float LastSeenTimestamp { get; set; }

    /// <summary>
    /// The strength of this memory when it was initially formed or last reinforced.
    /// This value is influenced by the importance of the vision type and contextual factors (e.g., needs).
    /// </summary>
    public float InitialStrength { get; set; }

    public MemoryVisionResult(CanBeSeenNode2D target, Vector2 position, VisionType type, float lastSeenTimestamp, float initialStrength)
        : base(target, position, type)
    {
        LastSeenTimestamp = lastSeenTimestamp;
        InitialStrength = initialStrength;
    }

    /// <summary>
    /// Calculates the current strength of the memory.
    /// Strength decays linearly over time from its InitialStrength.
    /// </summary>
    /// <param name="currentTime">The current game time in seconds.</param>
    /// <param name="decayPerSecond">The rate at which strength is lost per second.</param>
    /// <returns>The calculated current strength, clamped at a minimum of 0.</returns>
    public float GetCurrentStrength(float currentTime, float decayPerSecond)
    {
        if (InitialStrength <= 0) return 0; // A memory that starts with no strength has no strength.
        float timePassed = currentTime - LastSeenTimestamp;
        float currentStrength = InitialStrength - (timePassed * decayPerSecond);
        return Mathf.Max(currentStrength, 0f); // Strength is floored at 0
    }

    /// <summary>
    /// Calculates the current position accuracy radius for this memory.
    /// A stronger memory is more accurate (smaller radius).
    /// A fully decayed memory has the maximum accuracy radius.
    /// </summary>
    /// <param name="currentTime">The current game time in seconds.</param>
    /// <param name="decayPerSecond">The rate at which strength is lost per second (should match BrainComponent's rate).</param>
    /// <param name="maxAccuracyRadius">The largest possible radius of uncertainty, configured in BrainComponent.</param>
    /// <returns>The radius of uncertainty around the memory's Position. 0 for perfect accuracy.</returns>
    public float GetCurrentPositionAccuracy(float currentTime, float decayPerSecond, float maxAccuracyRadius)
    {
        if (InitialStrength <= 0) return maxAccuracyRadius; // No initial strength means max uncertainty

        float currentStrength = GetCurrentStrength(currentTime, decayPerSecond);
        if (currentStrength <= 0) return maxAccuracyRadius; // Fully decayed means max uncertainty

        // Accuracy is inversely proportional to strength percentage.
        // Strength ratio = currentStrength / InitialStrength (0 to 1)
        // Accuracy factor = 1 - strength_ratio (0 to 1, where 1 is max uncertainty)
        float strengthRatio = currentStrength / InitialStrength;
        float accuracyFactor = 1.0f - Mathf.Clamp(strengthRatio, 0f, 1f);

        return accuracyFactor * maxAccuracyRadius;
    }
}