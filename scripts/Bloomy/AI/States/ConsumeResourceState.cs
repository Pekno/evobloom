using Godot;
using System.Linq;

/// <summary>
/// Base state for consuming a resource when the Bloomy's interaction area overlaps it.
/// Invokes an action on the resource and updates the Bloomy accordingly.
/// </summary>
public abstract partial class ConsumeResourceState<TResource> : Node2D, IConditionalBehaviourState
    where TResource : CanBeSeenNode2D
{
    private bool _consumed = false;
    private BehaviourComponent _behaviour;

    [Export]
    public Area2D _interactionArea;

    protected virtual string TargetGroup { get; }
    public Node _nearbyResource;

    public override void _Ready()
    {
        _interactionArea.Connect("body_entered", new Callable(this, nameof(OnBodyEntered)));
        _interactionArea.Connect("body_exited", new Callable(this, nameof(OnBodyExited)));
    }

    private void OnBodyEntered(Node body)
    {
        if (body.IsInGroup(TargetGroup)) _nearbyResource = body;
    }

    private void OnBodyExited(Node body)
    {
        if (body.IsInGroup(TargetGroup) && _nearbyResource == body) _nearbyResource = null;
    }

    public void Enter(BehaviourComponent c)
    {
        _consumed = false;
        _behaviour = c;
        // Stop movement while consuming
        c.GetBodyPart<MuscleComponent>()?.StopMoving();
    }

    public void Process(BehaviourComponent c, float delta)
    {
        if (_consumed || _interactionArea == null || _nearbyResource == null) return;
        var ressourceNode = _nearbyResource.GetParent<TResource>();
        if (ressourceNode != null)
        {
            PerformConsume(ressourceNode, c);
            _consumed = true;
        }
    }

    protected abstract void PerformConsume(TResource nearbyResource, BehaviourComponent c);
    protected abstract bool IsFull(BioComponent bio);

    public void Exit(BehaviourComponent c) { _consumed = false; }

    public bool ShouldTransition(BehaviourComponent c)
    {
        // If Bloomy is already full, don't drink
        bool isFull = IsFull(c.GetBodyPart<BioComponent>());
        // ... and if the Bloomy as not already drinked the bush
        bool hasNotConsummed = !_consumed;
        // ... and if the Bloomy is near a bush
        bool isNearResource = _nearbyResource != null;

        return !isFull && hasNotConsummed && isNearResource;
    }
}
