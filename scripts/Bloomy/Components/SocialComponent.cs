using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Tracks which Bloomies this one has met (via an Area2D) and
/// queues up pending “conversations.” 
/// </summary>
public partial class SocialComponent : BloomyComponent
{
    [Export]
    public float MaxDiscussionDuration = 5.0f; // How long to talk to each other

    [Export]
    public float DiscussionCooldown = 20.0f; // How long to wait before talking again

    private float _discussionTimer = 0.0f; // Timer for the current discussion

    // All the other Bloomies we know
    private readonly Dictionary<Bloomy, float> _knownBloomies = new();
    // Queue of partners we still need to talk to
    private readonly Queue<Bloomy> _pending = new();

    public bool HasPendingConversation => _pending.Count > 0;
    private Bloomy NextConversationPartner => HasPendingConversation ? _pending.Peek() : null;

    private Action _onConversationEndCallback = null;

    [Signal]
    public delegate void MeetNewBloomyEventHandler(Bloomy bloomy);

    [Signal]
    public delegate void TalkWithBloomyEventHandler(Bloomy bloomy);


    [Signal]
    public delegate void ConversationEventEventHandler(int conversationStatus);


    public override void _Ready()
    {
        _discussionTimer = MaxDiscussionDuration;
    }

    public override void _Draw()
    {
        if (!Debug)
            return;

        Vector2 origin = ToLocal(GetParent().GetParent<Node2D>()?.GlobalPosition ?? GlobalPosition);

        foreach (var (knownBloomy, _) in _knownBloomies)
        {
            if (knownBloomy is Node2D targetNode && targetNode.IsInsideTree())
            {
                Vector2 targetPos = ToLocal(targetNode.GlobalPosition);
                if (CanTalkToTogether(knownBloomy))
                    DrawDashedLine(origin, targetPos, Colors.Blue, 1.0f);
                else
                    DrawLine(origin, targetPos, Colors.Blue, 1.0f);
            }
        }
    }

    public bool CanTalkTo(Bloomy partner)
    {
        float now = Time.GetTicksMsec() / 1000f;
        var alreadyKnow = _knownBloomies.ContainsKey(partner);
        // If we don’t know them yet, we can talk to them or if cooldown is over
        return !alreadyKnow || alreadyKnow && _knownBloomies[partner] < (now - DiscussionCooldown);
    }

    public bool CanTalkToTogether(Bloomy partner)
    {
        // Check if we can talk to the partner and if they can talk to us
        return CanTalkTo(partner) && partner.GetBodyPart<SocialComponent>().CanTalkTo(_bloomy);
    }

    public override void ProcessComponent(float delta)
    {
        if (HasPendingConversation)
        {
            _discussionTimer -= delta;

            // If we have a partner, start the conversation
            if (_discussionTimer <= 0.0f)
            {
                EngageConversation(NextConversationPartner);
                _discussionTimer = MaxDiscussionDuration;
                _pending.Dequeue(); // Remove the partner from the queue

                if (!HasPendingConversation)
                {
                    // If we have no more partners, stop the conversation
                    StopConversation();
                }
            }
        }

        QueueRedraw();
    }

    public void StartGroupConversation(List<Bloomy> nearOther, Action callback)
    {
        float now = Time.GetTicksMsec() / 1000f;
        _onConversationEndCallback = callback;
        // Add all the other Bloomies to the queue if we don’t know them yet
        foreach (var bloom in nearOther)
        {
            if (_pending.Contains(bloom))
                continue;

            _pending.Enqueue(bloom);

            // If we don’t know them yet, add them to the known list
            if (_knownBloomies.ContainsKey(bloom))
                continue;

            _knownBloomies.Add(bloom, now);

            EmitSignal(nameof(MeetNewBloomy), bloom);
        }

        if (HasPendingConversation)
        {
            EmitSignal(nameof(ConversationEvent), (int)ConversationStatus.Started);
        }
    }

    public void StopConversation()
    {
        // Clear the queue of pending conversations in case we’re interrupted
        _pending.Clear();
        EmitSignal(nameof(ConversationEvent), (int)ConversationStatus.Ended);
        // Trigger the callback to indicate that conversation has ended
        _onConversationEndCallback?.Invoke();
    }


    /// <summary>
    /// Call once you’ve had a conversation with the current partner.
    /// </summary>
    public void EngageConversation(Bloomy partner)
    {
        ExchangeMemories(partner);
        EmitSignal(nameof(TalkWithBloomy), partner);
    }

    /// <summary>
    /// Exchange long-term memories with the given partner.
    /// </summary>
    public void ExchangeMemories(Bloomy partner)
    {
        var myBrain = GetBodyPart<BrainComponent>();
        var partnerBrain = partner.GetBodyPart<BrainComponent>();
        if (myBrain == null || partnerBrain == null)
            return;

        //I import all of theirs… the other bloomy will do the same
        myBrain.ImportMemory(partnerBrain);
    }
}
