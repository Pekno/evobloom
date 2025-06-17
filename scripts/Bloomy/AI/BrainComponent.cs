using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Processes sensory memory, manages long-term memory with a decay system,
/// and selects the highest-weighted target based on physiological needs and memories.
/// </summary>
public partial class BrainComponent : BloomyComponent
{
    // Reference to BioComponent for need levels
    private BioComponent _bio;
    private GrowingComponent _growing;
    private DNAComponent _dna;

    // --- Memory System Parameters ---
    [ExportGroup("Memory System")]
    [Export] public float MaxPositionAccuracyRadius = 50f; // Max radius of uncertainty for weakest memories.
    [Export] public float BaseMemoryStrength = 100f; // Default initial strength for a standard memory.
    [Export] public float BaseMemoryDecayPerSecond = 2f;  // How much strength is lost per second from InitialStrength.
    [Export] public float MemoryReinforcementBonus = 30f; // Strength bonus when a known memory is re-sighted.
    [Export] public float MaxMemoryStrength = 250f; // Absolute cap for memory strength.
    [Export(PropertyHint.Range, "0,1")] public float ImportedMemoryStrengthFactor = 0.6f; // Factor of BaseMemoryStrength for imported memories.


    // --- Multipliers for initial memory strength based on VisionType's inherent importance ---
    [ExportGroup("Memory Strength Multipliers (vs BaseMemoryStrength)")]
    [Export(PropertyHint.Range, "0.1,5.0")] public float FruitTreeStrengthMultiplier = 1.5f;
    [Export(PropertyHint.Range, "0.1,5.0")] public float WaterStrengthMultiplier = 1.5f;
    [Export(PropertyHint.Range, "0.1,5.0")] public float ThreatStrengthMultiplier = 2.0f;
    [Export(PropertyHint.Range, "0.1,5.0")] public float FruitStrengthMultiplier = 0.7f; // Fruits are transient
    [Export(PropertyHint.Range, "0.1,5.0")] public float MateStrengthMultiplier = 1.0f;
    [Export(PropertyHint.Range, "0.1,5.0")] public float PredatorStrengthMultiplier = 2.5f;
    [Export(PropertyHint.Range, "0.1,5.0")] public float ShelterStrengthMultiplier = 1.2f;

    // --- Bonus strength added to memory if formed when a corresponding need is high ---
    [ExportGroup("Memory Need-Based Strength Bonus")]
    [Export] public float NeedDrivenStrengthBonus = 40f; // Flat bonus added if need is high for relevant memory type

    private List<VisionResult> shortTermMemory = new List<VisionResult>();
    private List<MemoryVisionResult> longTermMemory = new List<MemoryVisionResult>();

    private List<Feeling> _feelings;

    // Used for drawing normalization, stores the max initial strength encountered or base, whichever is higher.
    private float _maxObservedInitialStrength = 1f;

    [Signal]
    public delegate void BestTargetSelectedEventHandler(ProcessedVisionResult processedResult);

    [Signal]
    public delegate void FeelingSelectedEventHandler(int feeling);
    private FeelingType _lastEmittedFeeling = FeelingType.None;

    public FeelingType LastEmittedFeeling => _lastEmittedFeeling;
    public List<Feeling> GetFeelingsList() => _feelings;

    private Vector2 _lastClosestPosition = Vector2.Zero;
    private float CurrentTime => (float)Time.GetTicksMsec() / 1000f;

    private float ActualDecayPerSecond
    {
        get
        {
            float dnaFactor = _dna?.GetTraitValue(DNATraitType.MemoryDecayRateMultiplier) ?? 1.0f;
            float ageFactor = _growing?.GetCurrentMemoryDecayFactor() ?? 1.0f;
            return BaseMemoryDecayPerSecond * dnaFactor * ageFactor;
        }
    }

    private RandomNumberGenerator _rng = new RandomNumberGenerator();

    public override void _Ready()
    {
        _bio = GetBodyPart<BioComponent>();
        _growing = GetBodyPart<GrowingComponent>();
        if (_growing == null)
        {
            GD.PrintErr($"*{_bloomy?.Surname}* BrainComponent: GrowingComponent not found! Memory decay will not be affected by age.");
        }
        _dna = GetBodyPart<DNAComponent>();
        if (_dna == null)
        {
            GD.PrintErr($"*{_bloomy?.Surname}* BrainComponent: DNAComponent not found! Memory decay will not use genetic multiplier.");
        }
        _maxObservedInitialStrength = BaseMemoryStrength; // Initialize for drawing

        var sight = GetBodyPart<SightComponent>();
        if (sight != null)
            sight.Connect(SightComponent.SignalName.ThingsDetected, new Callable(this, nameof(OnThingsDetected)));
        else
            GD.PrintErr("BrainComponent: SightComponent not found!");

        _feelings = new List<Feeling>
        {
            new Hunger(),
            new Thirst(),
            //new Fear(), // Assuming Fear will be updated to use GetCurrentStrength
            //new Heat(), // Assuming Heat will be updated to use GetCurrentStrength
            new Boredom()
        };
    }

    public override void _Draw()
    {
        if (!Debug && !Engine.IsEditorHint() && !GetTree().DebugCollisionsHint) // Also check global debug hint
            return;

        Vector2 origin = ToLocal(GetParent().GetParent<Node2D>()?.GlobalPosition ?? GlobalPosition);
        float now = CurrentTime;
        float decayRate = ActualDecayPerSecond;

        // Draw Short-Term Memory (Blue)
        foreach (var memory in shortTermMemory)
        {
            if (memory.Target != null && memory.Target is CanBeSeenNode2D targetNode && targetNode.IsInsideTree())
            {
                Vector2 targetPos = ToLocal(targetNode.GlobalPosition);
                if (targetNode.IsConsidered())
                    DrawLine(origin, targetPos, Colors.Blue, 1.0f);
                else
                    DrawDashedLine(origin, targetPos, Colors.Blue, 1.0f, 4.0f);
            }
        }

        // Draw Long-Term Memory (Color-coded by strength: Green=Strong, Red=Weak/Decayed)
        foreach (var memory in longTermMemory)
        {
            if (memory.Target != null && memory.Target is CanBeSeenNode2D targetNode && targetNode.IsInsideTree())
            {
                Vector2 targetPos = ToLocal(targetNode.GlobalPosition); // This is the original remembered position
                float currentStrength = memory.GetCurrentStrength(now, decayRate);
                float strengthRatio = memory.InitialStrength > 0 ? Mathf.Clamp(currentStrength / memory.InitialStrength, 0f, 1f) : 0f;

                Color memoryColor = Colors.Red.Lerp(Colors.LimeGreen, strengthRatio);
                memoryColor.A = 0.2f + strengthRatio * 0.8f; // More opaque if stronger

                // Draw line to target
                if (targetNode.IsConsidered()) // Or perhaps just if currentStrength > 0
                    DrawLine(origin, targetPos, memoryColor, currentStrength > 0 ? 1.5f : 0.5f);
                else
                    DrawDashedLine(origin, targetPos, memoryColor, currentStrength > 0 ? 1.5f : 0.5f, 4.0f);

                // --- NEW: Draw Accuracy Circle ---
                if (currentStrength > 0) // Only draw accuracy for active memories
                {
                    float accuracyRadius = memory.GetCurrentPositionAccuracy(now, decayRate, MaxPositionAccuracyRadius);
                    if (accuracyRadius > 0)
                    {
                        Color accuracyColor = new Color(memoryColor, 0.15f); // Use memory color but more transparent
                        // Draw the circle around the *local* target position
                        DrawCircle(targetPos, accuracyRadius, accuracyColor);
                    }
                }
            }
        }
    }

    public override void ProcessComponent(float delta)
    {
        ProcessMemory(delta); // Pass delta for time-based processing if ever needed directly here
        ComputeFeelings();
        QueueRedraw();
    }

    private void OnThingsDetected(VisionResult[] visions)
    {
        float now = CurrentTime;

        foreach (var vision in visions)
        {
            var existingMemory = longTermMemory.FirstOrDefault(m => m.Target == vision.Target && m.Position.IsEqualApprox(vision.Position));

            if (existingMemory != null)
            {
                // Reinforce existing memory
                ProcessedVisionResult processedForReinforcement = ProcessVision(new VisionResult(vision.Target, vision.Position, vision.Type)); // Get its base calculated strength now
                existingMemory.InitialStrength = Mathf.Min(MaxMemoryStrength, processedForReinforcement.CalculatedInitialMemoryStrength + MemoryReinforcementBonus);
                existingMemory.LastSeenTimestamp = now;
                _maxObservedInitialStrength = Mathf.Max(_maxObservedInitialStrength, existingMemory.InitialStrength);
            }
            else
            {
                // If not in long-term, check if it's a duplicate in short-term before adding
                if (!shortTermMemory.Any(stm => stm.Target == vision.Target && stm.Position.IsEqualApprox(vision.Position)))
                {
                    shortTermMemory.Add(vision);
                }
            }
        }
    }

    public bool HasActiveMemory(VisionResult visionResult)
    {
        float now = CurrentTime;
        float decayRate = ActualDecayPerSecond;
        return longTermMemory.Any(m =>
            m.Target == visionResult.Target &&
            m.Position.IsEqualApprox(visionResult.Position) &&
            m.GetCurrentStrength(now, decayRate) > 0
        );
    }

    public void ProcessMemory(float delta) // Delta might be used for fine-grained timers if needed
    {
        float now = CurrentTime;
        float decayRate = ActualDecayPerSecond;

        // 1. Decay and Prune Long-Term Memories
        // It's generally better to remove from a list by iterating backwards or using RemoveAll.
        longTermMemory.RemoveAll(mem => mem.GetCurrentStrength(now, decayRate) <= 0);

        // 2. Process Short-Term Memory if any
        if (shortTermMemory.Any())
        {
            var memoryToProcess = new List<VisionResult>(shortTermMemory);
            shortTermMemory.Clear(); // Clear short-term once copied

            var processedResults = memoryToProcess
                .Where(x => x.Target != null)
                .Select(ProcessVision) // This now returns ProcessedVisionResult with CalculatedInitialMemoryStrength
                .Where(r => r.CalculatedInitialMemoryStrength > 0) // Only store memories with some initial strength
                .OrderByDescending(r => r.Weight) // Order by immediate decision weight for potential early out or priority
                .ToList();

            foreach (var pr in processedResults)
            {
                // Double-check if it was added to long-term memory by a concurrent OnThingsDetected reinforcement
                // This can happen if OnThingsDetected processes faster than ProcessMemory in a frame.
                var existing = longTermMemory.FirstOrDefault(m => m.Target == pr.Target && m.Position.IsEqualApprox(pr.Position));
                if (existing == null)
                {
                    longTermMemory.Add(new MemoryVisionResult(
                        pr.Target,
                        pr.Position,
                        pr.Type,
                        now, // LastSeenTimestamp
                        Mathf.Min(pr.CalculatedInitialMemoryStrength, MaxMemoryStrength) // Use the calculated strength, capped
                    ));
                    _maxObservedInitialStrength = Mathf.Max(_maxObservedInitialStrength, pr.CalculatedInitialMemoryStrength);
                }
                // If it exists, it means OnThingsDetected already reinforced it, so we don't add a duplicate.
            }
        }
        // Update _maxObservedInitialStrength after pruning and adding, in case max strength memory was removed
        if (longTermMemory.Any())
        {
            _maxObservedInitialStrength = longTermMemory.Select(m => m.InitialStrength).DefaultIfEmpty(BaseMemoryStrength).Max();
        }
        else
        {
            _maxObservedInitialStrength = BaseMemoryStrength;
        }
    }


    public void ComputeFeelings()
    {
        float now = CurrentTime;
        float decayRate = ActualDecayPerSecond;

        foreach (var f in _feelings)
            f.Compute(_bio, longTermMemory, now, decayRate); // Feelings will need to use GetCurrentStrength

        var best = _feelings.OrderByDescending(f => f.Weight).FirstOrDefault() ?? new Boredom();

        if (best.Type != _lastEmittedFeeling)
        {
            EmitSignal(nameof(FeelingSelected), (int)best.Type);
            _lastEmittedFeeling = best.Type;
        }
    }

    /// <summary>
    /// Computes the immediate decision-making weight and potential initial memory strength for a given vision.
    /// </summary>
    private ProcessedVisionResult ProcessVision(VisionResult vision)
    {
        float decisionWeight = VisionTypeProperties.GetBaseWeight(vision.Type); // This is for decision making, can be negative for threats
        float initialMemoryStrength = BaseMemoryStrength;

        // Adjust initial memory strength based on VisionType's inherent importance
        switch (vision.Type)
        {
            case VisionType.FruitTree: initialMemoryStrength *= FruitTreeStrengthMultiplier; break;
            case VisionType.Water: initialMemoryStrength *= WaterStrengthMultiplier; break;
            case VisionType.Threat: initialMemoryStrength *= ThreatStrengthMultiplier; break;
            case VisionType.Fruit: initialMemoryStrength *= FruitStrengthMultiplier; break;
            case VisionType.Mate: initialMemoryStrength *= MateStrengthMultiplier; break;
            case VisionType.Predator: initialMemoryStrength *= PredatorStrengthMultiplier; break;
            case VisionType.Shelter: initialMemoryStrength *= ShelterStrengthMultiplier; break;
        }

        // Retrieve physiological needs for adjustments
        float hunger = _bio?.HungerLevel ?? 0f;
        float thirst = _bio?.ThirstLevel ?? 0f;

        // Adjust decisionWeight and add bonus to initialMemoryStrength based on current needs
        switch (vision.Type)
        {
            case VisionType.FruitTree:
            case VisionType.Fruit:
                decisionWeight *= (1f + hunger * 2f);
                if (hunger > 0.5f) initialMemoryStrength += NeedDrivenStrengthBonus;
                break;
            case VisionType.Water:
                decisionWeight *= (1f + thirst * 2f);
                if (thirst > 0.5f) initialMemoryStrength += NeedDrivenStrengthBonus;
                break;
            case VisionType.Mate:
                decisionWeight *= (1f - hunger) * (1f - thirst); // Mates more interesting if needs met
                // No specific need-driven strength bonus here unless a "social" need is added
                break;
            case VisionType.Threat:
            case VisionType.Predator:
                // Threats are always highly significant for decisions.
                // Their base weight is already high/negative.
                // Their memory strength is already boosted by multiplier.
                // No additional need-based adjustment for decisionWeight usually, but could add if "fear" is a stat.
                break;
        }

        initialMemoryStrength = Mathf.Min(initialMemoryStrength, MaxMemoryStrength); // Cap initial strength

        // If the vision being processed is already a long-term memory,
        // its current strength should influence the decisionWeight.
        if (vision is MemoryVisionResult memVis)
        {
            float now = CurrentTime;
            float decayRate = ActualDecayPerSecond;
            float currentStrengthOfExistingMemory = memVis.GetCurrentStrength(now, decayRate);
            // Modulate decision weight by the reliability (current strength / initial strength) of the memory
            // This makes us trust stronger (fresher, more important) memories more for decisions.
            if (memVis.InitialStrength > 0)
            {
                decisionWeight *= (currentStrengthOfExistingMemory / memVis.InitialStrength);
            }
            else if (currentStrengthOfExistingMemory <= 0) // If it has no initial strength or is fully decayed
            {
                decisionWeight *= 0.1f; // Highly distrust memories that are somehow strengthless
            }
        }

        return new ProcessedVisionResult(vision, decisionWeight, initialMemoryStrength);
    }

    public void ImportMemory(BrainComponent otherBrain)
    {
        float now = CurrentTime;
        foreach (var memoryToImport in otherBrain.longTermMemory)
        {
            // Check if we already have this memory (don't care about its current strength, just if we know it)
            var existing = longTermMemory.FirstOrDefault(m => m.Target == memoryToImport.Target && m.Position.IsEqualApprox(memoryToImport.Position));

            if (existing == null)
            {
                // Not seen before, add it to our long-term memory with a default, possibly reduced, strength.
                // We don't know the context (needs) under which the other Bloomy formed this memory.
                // So, we assign a generic initial strength.
                float importedInitialStrength = BaseMemoryStrength * ImportedMemoryStrengthFactor;

                // Apply type-based multiplier for imported memories as well
                switch (memoryToImport.Type)
                {
                    case VisionType.FruitTree: importedInitialStrength *= FruitTreeStrengthMultiplier; break;
                    case VisionType.Water: importedInitialStrength *= WaterStrengthMultiplier; break;
                    case VisionType.Threat: importedInitialStrength *= ThreatStrengthMultiplier; break;
                    case VisionType.Fruit: importedInitialStrength *= FruitStrengthMultiplier; break;
                    case VisionType.Mate: importedInitialStrength *= MateStrengthMultiplier; break;
                    case VisionType.Predator: importedInitialStrength *= PredatorStrengthMultiplier; break;
                    case VisionType.Shelter: importedInitialStrength *= ShelterStrengthMultiplier; break;
                }

                importedInitialStrength = Mathf.Min(importedInitialStrength, MaxMemoryStrength); // Cap

                longTermMemory.Add(new MemoryVisionResult(
                    memoryToImport.Target,
                    memoryToImport.Position,
                    memoryToImport.Type,
                    now, // Timestamp of import becomes its LastSeenTimestamp
                    importedInitialStrength
                ));
                _maxObservedInitialStrength = Mathf.Max(_maxObservedInitialStrength, importedInitialStrength);
            }
            else
            {
                // We already know this. Optionally, we could reinforce our existing memory slightly,
                // or average its strength with the (estimated) strength of the imported one.
                // For now, just update timestamp if the imported one is "fresher" in a conceptual sense.
                // Or, more simply, a small reinforcement bonus to our existing memory.
                existing.InitialStrength = Mathf.Min(MaxMemoryStrength, existing.InitialStrength + MemoryReinforcementBonus * 0.5f); // Half bonus for "second-hand" info
                existing.LastSeenTimestamp = now; // Freshen our knowledge
                _maxObservedInitialStrength = Mathf.Max(_maxObservedInitialStrength, existing.InitialStrength);
            }
        }
    }

    public Vector2 GetBestTarget(IEnumerable<VisionType> targetTypes, Func<VisionResult, Node2D, float, float, float> scoreEvaluator)
    {
        var allCandidates = new List<VisionResult>();
        Node2D selfNode = GetParent<Node2D>();
        float now = CurrentTime;
        float decayRate = ActualDecayPerSecond;

        // Add considered items from short-term memory (always full strength conceptually for immediate processing)
        foreach (var type in targetTypes)
        {
            var recentForType = shortTermMemory
                .Where(v => v.Type == type && (v.Target?.IsConsidered() ?? false));
            allCandidates.AddRange(recentForType);
        }

        // Add active (non-decayed) considered items from long-term memory
        var activeLongTermMemories = longTermMemory
            .Where(m => targetTypes.Contains(m.Type) &&
                        (m.Target?.IsConsidered() ?? false) &&
                        m.GetCurrentStrength(now, decayRate) > 0);
        allCandidates.AddRange(activeLongTermMemories);


        if (!allCandidates.Any())
            return Vector2.Zero;

        // Score candidates. The scoreEvaluator will need to be aware of MemoryVisionResult
        // and use GetCurrentStrength if it wants to factor in memory reliability.
        var scoredCandidates = allCandidates
            .DistinctBy(c => c.Target)
            .Select(c => new
            {
                Candidate = c,
                Score = scoreEvaluator(c, selfNode, now, decayRate) // Pass now and decayRate
            })
            .OrderByDescending(sc => sc.Score)
            .ToList();

        if (!scoredCandidates.Any() || scoredCandidates[0].Score <= 0)
            return Vector2.Zero;

        var selectedScoredCandidate = scoredCandidates[0];

        // Avoid picking the exact same target *original position* if a viable alternative exists
        if (scoredCandidates.Count > 1 &&
            selectedScoredCandidate.Candidate.Position.IsEqualApprox(_lastClosestPosition))
        {
            if (scoredCandidates[1].Score > 0) // Ensure the second option is also viable
            {
                selectedScoredCandidate = scoredCandidates[1];
            }
        }

        // Store the *original* position for the anti-stuck logic
        _lastClosestPosition = selectedScoredCandidate.Candidate.Position;
        Vector2 finalTargetPosition = selectedScoredCandidate.Candidate.Position; // Default to original

        // Apply fuzziness if it's a long-term memory
        if (selectedScoredCandidate.Candidate is MemoryVisionResult memResult)
        {
            float accuracyRadius = memResult.GetCurrentPositionAccuracy(now, decayRate, MaxPositionAccuracyRadius);
            if (accuracyRadius > 0.01f) // Only apply fuzz if accuracy is meaningfully low (radius > 0.01)
            {
                _rng.Randomize(); // Ensure randomness if called multiple times in quick succession (though less likely now)
                float randomAngle = _rng.Randf() * Mathf.Pi * 2.0f; // Full circle
                float randomMagnitude = _rng.RandfRange(0, accuracyRadius); // Offset up to the accuracy radius

                Vector2 offset = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * randomMagnitude;
                finalTargetPosition += offset;
            }
        }

        return finalTargetPosition;
    }
}