using Godot;

public partial class Thought : GodotObject
{
    public float Timestamp { get; private set; }
    public string EventSource { get; private set; }
    public string Text { get; private set; }

    public Thought(string eventSource, string text)
    {
        Timestamp = (float)Time.GetTicksMsec() / 1000f; // Store seconds
        EventSource = eventSource;
        Text = text;
    }
}
