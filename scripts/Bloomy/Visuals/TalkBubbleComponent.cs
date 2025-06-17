using Godot;
using System;
using System.Collections.Generic;

public partial class TalkBubbleComponent : BloomyComponent
{
	[Export] public Marker2D BubblePosition;
	[Export] public PackedScene TalkBubbleScene;

	private Sprite2D _bubble;

	public override void _Ready()
	{
		if (TalkBubbleScene == null)
		{
			GD.PrintErr("TalkBubbleComponent: TalkBubbleScene not set!");
			return;
		}

		var socialComponent = GetBodyPart<SocialComponent>();
		if (socialComponent != null)
		{
			socialComponent.Connect(SocialComponent.SignalName.ConversationEvent, new Callable(this, nameof(OnConversationEvent)));
		}
		else
		{
			GD.PrintErr("TalkBubbleComponent: SocialComponent not found!");
		}

		var talkBubble = TalkBubbleScene.Instantiate<Node2D>();
		talkBubble.Position = BubblePosition.Position;
		AddChild(talkBubble);
		_bubble = talkBubble.GetNode<Sprite2D>("%Bubble");
	}

	public override void ProcessComponent(float delta)
	{ }


	private void OnConversationEvent(ConversationStatus status)
	{
		switch (status)
		{
			case ConversationStatus.Started:
				_bubble.Visible = true;
				break;
			case ConversationStatus.Ended:
				_bubble.Visible = false;
				break;
			default:
				break;
		}
	}
}