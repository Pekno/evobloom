using Godot;

public interface IBehaviourState
{
    /// <summary>
    /// Called when entering the state.
    /// </summary>
    void Enter(BehaviourComponent component);

    /// <summary>
    /// Processes the state logic each frame.
    /// </summary>
    void Process(BehaviourComponent component, float delta);

    /// <summary>
    /// Called when exiting the state.
    /// </summary>
    void Exit(BehaviourComponent component);
}
