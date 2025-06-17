using Godot;
using System.Linq;
using System.Collections.Generic;
using System;

/// <summary>
/// Orchestrates Bloomy states and reacts to Brain and BioComponent signals.
/// Now listens for BestTargetSelected to set a desired position and HungerThresholdReached to trigger MoveState.
/// </summary>
public partial class BehaviourComponent : BloomyComponent
{
    [Export] public float WanderRadiusMax = 200f;
    [Export] public float ChangeDirectionInterval = 2f;
    [Export] public float ConditionCheckCooldown = 0.5f;
    [Export] public NodePath DefaultStatePath;
    [Export] public NodePath MuscleComponentPath;
    [Export] public float StateLockDuration = 0.2f;

    private MuscleComponent _muscle;
    private BrainComponent _brain;
    public Vector2 Velocity => _muscle?.CurrentVelocity ?? Vector2.Zero;
    public float StateTimer { get; set; }

    private FeelingType _currentFeeling;

    public FeelingType CurrentFeeling { get => _currentFeeling; }
    private IBehaviourState _currentState;
    private IConditionalBehaviourState _defaultState;

    public IConditionalBehaviourState DefaultState => _defaultState;
    private readonly List<IConditionalBehaviourState> _stateNodes = new();

    private float _checkTimer = 0f;
    private float _lockTimer = 0f;

    /// <summary>Position the Brain selected as next goal.</summary>
    public Vector2 DesiredTargetPosition { get; private set; }
    /// <summary>True if Brain has issued a move command.</summary>
    public bool HasDesiredTarget { get => DesiredTargetPosition != Vector2.Zero; }

    /// <summary>Reset the move-to flag after arrival.</summary>
    public void ClearDesiredTarget()
    {
        DesiredTargetPosition = Vector2.Zero;
    }

    public override void _Ready()
    {
        // Resolve muscle reference
        _muscle = MuscleComponentPath != null
            ? GetNode<MuscleComponent>(MuscleComponentPath)
            : GetBodyPart<MuscleComponent>();
        if (_muscle == null)
            GD.PrintErr("MuscleComponent: MuscleComponent missing");

        // Grab Brain and listen for feelings
        _brain = GetBodyPart<BrainComponent>();
        if (_brain != null)
        {
            _brain.Connect(
                nameof(BrainComponent.FeelingSelected),
                new Callable(this, nameof(OnFeelingSelected))
            );
        }
        else
        {
            GD.PrintErr("BehaviourComponent: BrainComponent not found!");
        }

        // Collect conditional state nodes
        foreach (Node child in GetChildren())
            if (child is IConditionalBehaviourState s)
                _stateNodes.Add(s);

        // Set up default state
        _defaultState = GetNodeOrNull<IConditionalBehaviourState>(DefaultStatePath);
        if (_defaultState == null)
        {
            GD.PrintErr("BehaviourComponent: DefaultStatePath invalid");
            return;
        }

        ChangeState(_defaultState);
    }

    public T GetState<T>() where T : IBehaviourState
    {
        return _stateNodes.OfType<T>().FirstOrDefault();
    }

    public override void ProcessComponent(float delta)
    {
        float dt = (float)delta;
        _currentState?.Process(this, dt);

        _lockTimer = Mathf.Max(_lockTimer - dt, 0f);
        _checkTimer -= dt;

        if (_lockTimer == 0f && _checkTimer <= 0f)
        {
            _checkTimer = ConditionCheckCooldown;
            EvaluateTransitions();
        }
    }

    public void ChangeState(IBehaviourState newState)
    {
        if (newState == _currentState)
            return;

        if (Debug)
            GD.Print($"*{_bloomy.Surname}* - [Behaviour] {_currentState?.GetType().Name} -> {newState.GetType().Name}");

        _currentState?.Exit(this);
        _currentState = newState;
        _currentState.Enter(this);
        _lockTimer = StateLockDuration;

        // If swapping to idle state, redo a feeling action
        if (_currentState is IdleState)
        {
            OnFeelingSelected(_currentFeeling);
        }
    }

    private void EvaluateTransitions()
    {
        foreach (var st in _stateNodes)
            if (st != _currentState && st.ShouldTransition(this))
            { ChangeState(st); return; }
    }

    private void OnFeelingSelected(FeelingType feeling)
    {
        _currentFeeling = feeling;
        // Map each feeling to a VisionType and move
        switch (feeling)
        {
            case FeelingType.Hunger:
                var tryGetClosest = _brain.GetBestTarget([VisionType.Fruit, VisionType.FruitTree], TargetScoreEvaluators.Food);
                // If no berry is found, try to get to a random point else go to berry
                DesiredTargetPosition = tryGetClosest == Vector2.Zero
                    ? GetRandomWanderPoint()
                    : tryGetClosest;
                break;
            case FeelingType.Thirst:
                tryGetClosest = _brain.GetBestTarget([VisionType.Water], TargetScoreEvaluators.Water);
                DesiredTargetPosition = tryGetClosest == Vector2.Zero
                    ? GetRandomWanderPoint()
                    : tryGetClosest;
                break;
            // case FeelingType.Fear:
            //     DesiredTargetPosition = _brain.GetClosest(VisionType.Threat);
            //     HasDesiredTarget = DesiredTargetPosition != Vector2.Zero;
            //     break;
            case FeelingType.Boredom:
                DesiredTargetPosition = GetRandomWanderPoint();
                break;
            default:
                return; // stay in Idle
        }
    }

    public void GrabbedAction(bool isGrabbed)
    {
        Disabled = isGrabbed;
        if (isGrabbed)
        {
            ClearDesiredTarget();
            ChangeState(_defaultState);
        }
        else
        {
            OnFeelingSelected(_currentFeeling);
        }
    }

    /// <summary>
    /// Picks a random destination within WanderRadius of the Bloomy’s current position.
    /// </summary>
    private Vector2 GetRandomWanderPoint()
    {
        var nav = GetBodyPart<NavigationComponent>();
        // If navigation exists use it, if not rely on "old school" random position
        if (nav != null)
        {
            return nav.GetClosestUnvisitedChunkRandomPosition(GlobalPosition);
        }
        else
        {
            var rng = new Random();
            float angle = (float)(rng.NextDouble() * Math.PI * 2);
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (float)(Math.Max(rng.NextDouble(), 0.2) * WanderRadiusMax);
            // Bloomy’s world position:
            Vector2 origin = GetParent<Node2D>().GlobalPosition;
            return origin + offset;
        }
    }
}
