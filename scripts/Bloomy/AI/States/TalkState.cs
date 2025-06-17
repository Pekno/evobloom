using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class TalkState : Node2D, IConditionalBehaviourState
{
    private BehaviourComponent _behaviour;
    private SocialComponent _social;
    private bool _hasTalked = false;
    private bool _isTalking = false;

    [Export]
    public Area2D _talkArea;

    private List<Bloomy> _nearOther = new List<Bloomy>();

    public override void _Ready()
    {
        _behaviour = GetParent<BehaviourComponent>();
        _talkArea.Connect("body_entered", new Callable(this, nameof(OnBodyEntered)));
        _talkArea.Connect("body_exited", new Callable(this, nameof(OnBodyExited)));
    }

    private void OnBodyEntered(Node body)
    {
        // 1) If the body that entered is not in the “Bloomy” group, ignore it:
        if (!body.IsInGroup("Bloomy"))
            return;

        // 2) Get the Bloomy root that this TalkState lives under:
        var selfBloomy = _behaviour.GetParent<Bloomy>();
        var bloomy = body.GetNode<Bloomy>("%Bloomy");

        // 3) If the body that entered *is* your own Bloomy, ignore it:
        if (bloomy == selfBloomy)
            return;

        // 4) Otherwise, if it's a boomy, add it:
        if (bloomy != null && selfBloomy.GetBodyPart<SocialComponent>().CanTalkToTogether(bloomy))
            _nearOther.Add(bloomy);
    }

    private void OnBodyExited(Node body)
    {
        var bloomy = body.GetNodeOrNull<Bloomy>("Bloomy");

        if (bloomy != null && body.IsInGroup("Bloomy"))
        {
            _nearOther.Remove(bloomy);
        }
    }
    public void Enter(BehaviourComponent c)
    {
        _behaviour = c;
        _social = c.GetBodyPart<SocialComponent>();
        _hasTalked = false;
        _isTalking = false;

        // Pause movement
        c.GetBodyPart<MuscleComponent>()?.StopMoving();
        c.ClearDesiredTarget();
    }

    public void Process(BehaviourComponent c, float delta)
    {
        if (_isTalking || _social == null)
            return;

        _social.StartGroupConversation(_nearOther, () =>
        {
            _hasTalked = true;
            _isTalking = false;
            // Act as if no one is nearby anymore, so we don't get stuck in this state.
            _nearOther.Clear();
        });
        // Set the the isTalking flag once the group conversation started too prevent rerunning the same conversation.
        _isTalking = true;
    }



    public void Exit(BehaviourComponent c)
    {
        _hasTalked = false;
    }

    public bool ShouldTransition(BehaviourComponent c)
    {
        // Must have a partner waiting…
        bool hasNoPendingConversation = _social?.HasPendingConversation ?? false;
        // …and only if we’re currently bored…
        bool isBored = c?.CurrentFeeling == FeelingType.Boredom;
        // …and we’re near the other Bloomy
        bool isNearOther = _nearOther.Count > 0;

        // If asn't already have a partner, is bored and is near another Bloomy TalkState.
        return (!_hasTalked && !hasNoPendingConversation && isBored && isNearOther);
    }
}
