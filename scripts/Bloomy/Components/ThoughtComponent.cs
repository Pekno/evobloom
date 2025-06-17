using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public partial class ThoughtComponent : BloomyComponent
{
    public List<Thought> Thoughts { get; private set; } = new List<Thought>();

    [Signal]
    public delegate void ThoughtsChangedEventHandler(Thought newThought);

    public override void _Ready()
    {
        // Define (component, signalName, handlerName) tuples:
        var bindings = new List<(Type compType, string signal, string handler)>
        {
            (typeof(SightComponent), SightComponent.SignalName.ThingsDetected,nameof(OnThingsDetected)),
            (typeof(BrainComponent), BrainComponent.SignalName.BestTargetSelected, nameof(OnBestTargetSelected)),
            (typeof(BrainComponent), BrainComponent.SignalName.FeelingSelected, nameof(OnFeelingSelected)),
            (typeof(BioComponent), BioComponent.SignalName.Ate, nameof(OnAte)),
            (typeof(BioComponent), BioComponent.SignalName.Drank, nameof(OnDrank)),
            (typeof(SocialComponent), SocialComponent.SignalName.MeetNewBloomy, nameof(OnMeetNewBloomy)),
            (typeof(SocialComponent), SocialComponent.SignalName.TalkWithBloomy, nameof(OnTalkWithBloomy)),
        };

        // 1) Find the generic method definition
        var method = typeof(BloomyComponent)
            .GetMethod(
                nameof(GetBodyPart),
                BindingFlags.Instance | BindingFlags.Public
            );
        if (method == null)
            throw new InvalidOperationException("Could not find GetBodyPart<T>() method.");

        // Iterate and connect
        foreach (var (compType, signal, handler) in bindings)
        {
            // Create the closed‐generic MethodInfo
            var generic = method.MakeGenericMethod(compType);
            // Invoke it (no parameters) on this instance
            var comp = (IBloomyComponent)generic.Invoke(this, null);

            // Get the body-part instance
            if (comp is Node node)
            {
                node.Connect(signal, new Callable(this, handler));
            }
            else
            {
                GD.PrintErr($"ThoughtComponent: could not find {compType.Name} to connect '{signal}'");
            }
        }
    }

    public override void ProcessComponent(float delta) { }

    private void OnThingsDetected(VisionResult[] visions)
    {
        // Group by vision type, and for each group show "Type xN" if N>1, else just "Type"
        var summaries = visions
            .GroupBy(v => v.Type)
            .Select(g =>
                g.Count() > 1
                    ? $"{g.Key} x{g.Count()}"
                    : g.Key.ToString()
            ).ToArray();

        string text = $"I just saw {summaries.Join(", ")} !";
        AddThought("SomethingDetected", text);
    }

    private void OnBestTargetSelected(ProcessedVisionResult processed)
    {
        string decisionText;
        if (processed.Weight > 0)
        {
            switch (processed.Type)
            {
                case VisionType.Fruit:
                    decisionText = $"This {processed.Type} might be interesting, I will remember it for later.";
                    break;
                case VisionType.Threat:
                    decisionText = $"This {processed.Type} is close, I should avoid it.";
                    break;
                case VisionType.Water:
                    decisionText = $"Found {processed.Type}, that's useful if thirsty.";
                    break;
                default:
                    decisionText = $"Considering {processed.Type} as next objective.";
                    break;
            }
        }
        else
        {
            decisionText = $"This {processed.Type} seems irrelevant for now.";
        }

        AddThought("BestTargetSelected", decisionText);
    }

    private void OnAte()
    {
        AddThought("Ate", "MMmmh Yumy");
    }

    private void OnDrank()
    {
        AddThought("Drank", "*Slurp* *slurp*");
    }

    private void OnFeelingSelected(int feelingInt)
    {
        // Cast back into our enum
        var feeling = (FeelingType)feelingInt;
        AddThought("FeelingSelected", $"I'm feeling {feeling}");
    }

    private void OnMeetNewBloomy(Bloomy newBloomy)
    {
        AddThought("MeetNewBloomy", $"I just met with {newBloomy.Surname}!");
    }

    private void OnTalkWithBloomy(Bloomy otherBloomy)
    {
        AddThought("TalkWithBloomy", $"I just talked with {otherBloomy.Surname}!");
    }

    private void AddThought(string eventSource, string text)
    {
        var thought = new Thought(eventSource, text);
        Thoughts.Add(thought);

        if (Debug)
            GD.Print($"*{_bloomy.Surname}* - [Thought] {text}");

        EmitSignal(nameof(ThoughtsChanged), thought);
    }
}
