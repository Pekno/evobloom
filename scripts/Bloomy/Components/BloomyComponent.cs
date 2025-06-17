using Godot;

public abstract partial class BloomyComponent : Node2D, IBloomyComponent
{
    [Export]
    public bool Debug
    {
        get => _debug; set
        {
            _debug = value;
            TriggerDebug(value);
        }
    }

    [Export]
    public bool Disabled { get; set; } = false;

    protected Bloomy _bloomy { get => GetNode<Bloomy>($"%{nameof(Bloomy)}"); }

    protected bool _debug = false;

    public abstract void ProcessComponent(float delta);

    public virtual void TriggerDebug(bool value)
    {
        // Override this method in derived classes to implement debug behavior
    }

    public T GetBodyPart<T>() where T : IBloomyComponent
    {
        return _bloomy.GetBodyPart<T>();
    }
}