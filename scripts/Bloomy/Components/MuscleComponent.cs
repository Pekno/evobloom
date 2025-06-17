using Godot;

public partial class MuscleComponent : BloomyComponent
{
    [Export] public CharacterBody2D Body2D;
    [Export] public NavigationAgent2D Agent;
    [Export] public float SpeciesBaseMaxSpeed = 100f;

    // Stall‐detection exports
    [Export] public float StallCheckInterval = 1.0f;      // seconds between checks
    [Export] public float StallDistanceThreshold = 5f;    // minimum movement in pixels

    // Internal for stall detection
    private Vector2 _lastPosition;
    private float _stallTimer = 0f;

    // Tracks the current orientation (unit vector of movement)
    private Vector2 _orientation = Vector2.Zero;

    public Vector2 CurrentVelocity => Body2D?.Velocity ?? Vector2.Zero;
    private bool _hasPath;
    private GrowingComponent _growing;
    private DNAComponent _dna;
    private float CurrentMaxSpeed
    {
        get
        {
            float dnaFactor = _dna?.GetTraitValue(DNATraitType.SpeedMultiplier) ?? 1.0f;
            float ageFactor = _growing?.GetCurrentSpeedFactor() ?? 1.0f;
            return SpeciesBaseMaxSpeed * dnaFactor * ageFactor;
        }
    }

    public override void TriggerDebug(bool value)
    {
        Agent.DebugEnabled = value;
    }

    public override void _EnterTree()
    {
        Body2D ??= GetParent<CharacterBody2D>();
        Agent ??= GetNodeOrNull<NavigationAgent2D>("NavigationAgent2D");
        if (Body2D == null) GD.PrintErr("MuscleComponent: Body2D missing");
        if (Agent == null) GD.PrintErr("MuscleComponent: NavigationAgent2D missing");
    }

    public override void _Ready()
    {
        _growing = GetBodyPart<GrowingComponent>();
        if (_growing == null)
        {
            GD.PrintErr($"*{_bloomy?.Surname}* MuscleComponent: GrowingComponent not found! Speed will not be affected by age.");
        }
        _dna = GetBodyPart<DNAComponent>();
        if (_dna == null)
        {
            GD.PrintErr($"*{_bloomy?.Surname}* MuscleComponent: DNAComponent not found! Speed will not use genetic multiplier.");
        }
        // initialize stall tracking & orientation
        if (Body2D != null)
            _lastPosition = Body2D.GlobalPosition;
        _orientation = Vector2.Zero;
    }

    public void MoveTo(Vector2 globalTarget)
    {
        if (Agent == null) return;
        Agent.TargetPosition = globalTarget;
        _hasPath = true;

        // reset stall tracking
        if (Body2D != null)
        {
            _lastPosition = Body2D.GlobalPosition;
            _stallTimer = 0f;
        }
    }

    public void StopMoving()
    {
        _hasPath = false;
        if (Agent != null)
        {
            Agent.SetVelocity(Vector2.Zero);
            Agent.TargetPosition = Body2D.GlobalPosition;
        }
        if (Body2D != null)
            Body2D.Velocity = Vector2.Zero;

        // reset stall tracking
        if (Body2D != null)
            _lastPosition = Body2D.GlobalPosition;
        _stallTimer = 0f;

        // clear orientation when stopped
        _orientation = Vector2.Zero;
    }

    /// <summary>
    /// Returns the current movement direction as a unit Vector2.
    /// Zero vector if not moving.
    /// </summary>
    public Vector2 GetOrientation() => _orientation;

    public bool HasArrived() =>
        Agent != null && (Agent.IsTargetReached() || Agent.IsNavigationFinished());

    public override void ProcessComponent(float delta)
    {
        if (Body2D == null || Agent == null) return;

        // If path is finished, clear the moving flag
        if (_hasPath && Agent.IsNavigationFinished())
            _hasPath = false;

        if (_hasPath)
        {
            // Compute next direction
            Vector2 next = Agent.GetNextPathPosition();
            Vector2 dir = (next - Body2D.GlobalPosition).Normalized();

            // Update velocity and orientation
            Body2D.Velocity = dir * CurrentMaxSpeed;
            Agent.SetVelocity(Body2D.Velocity);
            _orientation = dir;
        }
        else
        {
            StopMoving();
        }

        Body2D.MoveAndSlide();

        // After updating position, mark the chunk as visited:
        var nav = GetBodyPart<NavigationComponent>();
        nav?.MarkVisited(Body2D.GlobalPosition);

        // --- stall detection ---
        if (_hasPath)
        {
            _stallTimer += delta;
            if (_stallTimer >= StallCheckInterval)
            {
                Vector2 currentPos = Body2D.GlobalPosition;
                float dist = currentPos.DistanceTo(_lastPosition);

                if (dist < StallDistanceThreshold)
                    StopMoving();

                // reset for next interval
                _lastPosition = currentPos;
                _stallTimer = 0f;
            }
        }
        else
        {
            // not moving, reset timer
            _stallTimer = 0f;
        }

        // Fallback: if velocity exists but orientation wasn’t set (e.g. manual velocity),
        // update orientation from velocity
        if (!_hasPath && Body2D.Velocity.LengthSquared() > 0f)
            _orientation = Body2D.Velocity.Normalized();
    }
}
