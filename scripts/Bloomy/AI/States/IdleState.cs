using Godot;

public partial class IdleState : Node2D, IConditionalBehaviourState
{
    private MuscleComponent _muscle;


    public void Enter(BehaviourComponent c)
    {
        // Grab Sight and check around
        var _sight = c.GetBodyPart<SightComponent>();
        if (_sight != null)
        {
            _sight.CheckAround();
        }
        else
        {
            GD.PrintErr("BehaviourComponent: BrainComponent not found!");
        }

        _muscle = c.GetBodyPart<MuscleComponent>();
        _muscle?.StopMoving();
        c.ClearDesiredTarget();
    }

    public void Process(BehaviourComponent c, float dt)
    {
        // Idle simply waits its full duration
    }

    public void Exit(BehaviourComponent _) { }
    public bool ShouldTransition(BehaviourComponent c)
    {
        // If we’ve just arrived (MoveState cleared the target),
        // then Idle should now take over immediately.
        return !c.HasDesiredTarget && c.GetBodyPart<MuscleComponent>().HasArrived() && c.GetBodyPart<SocialComponent>().HasPendingConversation == false;
    }
}
