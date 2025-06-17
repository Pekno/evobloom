public interface IConditionalBehaviourState : IBehaviourState
{
    /// <summary>
    /// Returns true if the state’s condition is met and the MovementComponent should switch to this state.
    /// </summary>
    bool ShouldTransition(BehaviourComponent component);
}
