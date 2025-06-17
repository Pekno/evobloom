using Godot;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Root Bloomy node: finds all IBloomyComponent implementations in its subtree
/// and updates them each frame.
/// </summary>
public partial class Bloomy : Node2D
{
	// The Bloomy’s unique name
	public string Surname { get; private set; }
	private readonly List<IBloomyComponent> components = new List<IBloomyComponent>();

	[Export]
	public bool Debug
	{
		get => _debug; set
		{
			foreach (var comp in components)
			{
				comp.Debug = value;
			}
			_debug = value;
		}
	}

	private bool _debug = false;

	public override void _EnterTree()
	{
		// Collect all components in the node tree
		CollectComponents(this);
	}

	public override void _Ready()
	{
		// Assign a random, procedurally generated name
		Surname = BloomyNameGenerator.GenerateName();
		GD.Print($"A new Bloomy has spawned: {Surname}!");

		// Re-enforce debug state if assigned before getting all components
		Debug = _debug;
	}

	public T GetBodyPart<T>() where T : IBloomyComponent
	{
		return components.OfType<T>().FirstOrDefault();
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		// Process each component with the frame delta
		foreach (var comp in components)
		{
			if (comp.Disabled) continue;
			comp.ProcessComponent(dt);
		}
	}

	/// <summary>
	/// Walks the node tree under 'node' and adds every IBloomyComponent found.
	/// </summary>
	private void CollectComponents(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is IBloomyComponent bloomyComp)
			{
				components.Add(bloomyComp);
			}

			// Recurse into grandchildren
			CollectComponents(child);
		}
	}
}
