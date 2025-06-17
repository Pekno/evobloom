using Godot;
using System; // For Func

public static class TargetScoreEvaluators
{
    // --- Configuration Constants ---
    private const float BaseFruitPreference = 100.0f;
    private const float BaseFruitTreePreference = 70.0f;
    private const float FoodDistancePenaltyFactor = 0.05f;

    private const float BaseWaterPreference = 120.0f;
    private const float WaterDistancePenaltyFactor = 0.02f;

    /// <summary>
    /// Evaluates the score of a potential food source.
    /// Considers type, distance, and memory strength (reliability).
    /// </summary>
    public static float Food(VisionResult visionResult, Node2D self, float currentTime, float decayRate)
    {
        float score = 0;
        float distance = visionResult.Position.DistanceTo(self.GlobalPosition);

        switch (visionResult.Type)
        {
            case VisionType.Fruit:
                score = BaseFruitPreference;
                break;
            case VisionType.FruitTree:
                score = BaseFruitTreePreference;
                break;
            default:
                return 0; // Not a relevant food type
        }

        // Apply distance penalty
        if (distance > float.Epsilon)
        {
            score /= (1.0f + distance * FoodDistancePenaltyFactor);
        }

        // Factor in memory reliability for long-term memories
        if (visionResult is MemoryVisionResult memVis)
        {
            if (memVis.InitialStrength > 0)
            {
                float currentStrength = memVis.GetCurrentStrength(currentTime, decayRate);
                // If strength is 0, reliability is 0. Filtered out by GetBestTarget, but defensive.
                float reliability = currentStrength / memVis.InitialStrength;
                score *= Mathf.Clamp(reliability, 0f, 1f);
            }
            else
            {
                // Memory with no initial strength is considered unreliable for scoring.
                // Or if it somehow has strength without initial strength (should not happen with new logic).
                float currentStrength = memVis.GetCurrentStrength(currentTime, decayRate);
                if (currentStrength <= 0) score = 0f; // Definitely no score if no strength
                // else, if it has current strength but no initial, treat as somewhat unreliable
                // This case should ideally be avoided by ensuring InitialStrength is always positive for valid memories.
            }
        }
        // If not a MemoryVisionResult (i.e., from short-term memory), it's considered 100% reliable for this evaluation.

        return score;
    }

    /// <summary>
    /// Evaluates the score of a potential water source.
    /// Considers distance and memory strength (reliability).
    /// </summary>
    public static float Water(VisionResult visionResult, Node2D self, float currentTime, float decayRate)
    {
        float score = 0;
        float distance = visionResult.Position.DistanceTo(self.GlobalPosition);

        if (visionResult.Type == VisionType.Water)
        {
            score = BaseWaterPreference;
        }
        else
        {
            return 0; // Not a water type
        }

        // Apply distance penalty
        if (distance > float.Epsilon)
        {
            score /= (1.0f + distance * WaterDistancePenaltyFactor);
        }

        // Factor in memory reliability for long-term memories
        if (visionResult is MemoryVisionResult memVis)
        {
            if (memVis.InitialStrength > 0)
            {
                float currentStrength = memVis.GetCurrentStrength(currentTime, decayRate);
                float reliability = currentStrength / memVis.InitialStrength;
                score *= Mathf.Clamp(reliability, 0f, 1f);
            }
            else
            {
                float currentStrength = memVis.GetCurrentStrength(currentTime, decayRate);
                if (currentStrength <= 0) score = 0f;
            }
        }
        // If not a MemoryVisionResult (i.e., from short-term memory), it's considered 100% reliable.

        return score;
    }

    // Add more evaluators as needed, e.g., Shelter, Mate, ThreatAvoidanceScore
    // public static float Shelter(VisionResult visionResult, Node2D self, float currentTime, float decayRate) { ... }
}