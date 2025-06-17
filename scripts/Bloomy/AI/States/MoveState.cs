using Godot;

/// <summary>
/// Moves the Bloomy toward a desired target position set by the BrainComponent.
/// </summary>
public partial class MoveState : Node2D, IConditionalBehaviourState
{
    private BehaviourComponent _behaviour;
    private MuscleComponent _muscle;

    public void Enter(BehaviourComponent c)
    {
        _behaviour = c;
        _muscle = c.GetBodyPart<MuscleComponent>();
        if (_muscle != null && c.HasDesiredTarget)
        {
            _muscle.MoveTo(c.DesiredTargetPosition);
        }
    }

    public void Process(BehaviourComponent c, float delta)
    {
        // once we've stopped moving, clear the desire to move
        if (_muscle != null && !_muscle.HasArrived())
        {
            c.ClearDesiredTarget();
        }
    }

    public void Exit(BehaviourComponent c)
    {
        c.ClearDesiredTarget();
    }

    public bool ShouldTransition(BehaviourComponent c)
    {
        // Transition to MoveState if a target is desired and movement hasn't started yet
        if (c.HasDesiredTarget)
        {
            return true;
        }
        // Transition out of MoveState when target reached
        return _muscle != null && !_muscle.HasArrived();
    }
}
