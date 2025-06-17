using Godot;

public interface IBloomyComponent
{
    bool Debug { get; set; }
    bool Disabled { get; set; }
    /// <summary>
    /// Called by the parent Bloomy during the _Process method.
    /// </summary>
    /// <param name="delta">The frame time converted to float.</param>
    void ProcessComponent(float delta);
}
