using Godot;
using System;
using System.Collections.Generic;

public partial class FloatingThoughtComponent : BloomyComponent
{
	[Export] public Vector2 Offset = new Vector2(0, -10);
	[Export] public float DisplayDuration = 3.0f;
	[Export] public PackedScene LabelScene;

	private Queue<Thought> thoughtQueue = new Queue<Thought>();
	private List<Label> activeLabels = new List<Label>();
	private List<float> labelTimers = new List<float>();

	public override void _Ready()
	{
		if (LabelScene == null)
		{
			GD.PrintErr("FloatingThoughtComponent: LabelScene not set!");
			return;
		}

		var thoughtComponent = GetBodyPart<ThoughtComponent>();
		if (thoughtComponent != null)
		{
			thoughtComponent.Connect(ThoughtComponent.SignalName.ThoughtsChanged, new Callable(this, nameof(OnNewThought)));
		}
		else
		{
			GD.PrintErr("FloatingThoughtComponent: ThoughtComponent not found!");
		}
	}

	public override void ProcessComponent(float delta)
	{
		for (int i = activeLabels.Count - 1; i >= 0; i--)
		{
			labelTimers[i] -= delta;

			if (labelTimers[i] <= 0f)
			{
				activeLabels[i].QueueFree();
				activeLabels.RemoveAt(i);
				labelTimers.RemoveAt(i);
				continue;
			}

			float progress = 1.0f - (labelTimers[i] / DisplayDuration);
			Label label = activeLabels[i];
			label.Position = Offset + new Vector2(0, (progress * -30) - (i * 20)) - new Vector2(label.Size[0] / 2, 0);
			label.Modulate = new Color(1, 1, 1, 1.0f - progress);
		}

		while (thoughtQueue.Count > 0 && activeLabels.Count < 3)
		{
			ShowNextThought();
		}
	}

	private void OnNewThought(Thought thought)
	{
		thoughtQueue.Enqueue(thought);
	}

	private void ShowNextThought()
	{
		if (thoughtQueue.Count == 0 || activeLabels.Count >= 3)
			return;

		var nextThought = thoughtQueue.Dequeue();

		Label newLabel = LabelScene.Instantiate<Label>();
		AddChild(newLabel);

		newLabel.Text = nextThought.Text;
		newLabel.Position = Offset - new Vector2(newLabel.Size[0] / 2, 0);
		newLabel.Modulate = new Color(1, 1, 1, 1);
		newLabel.ZIndex = 100;

		activeLabels.Add(newLabel);
		labelTimers.Add(DisplayDuration);
	}
}
